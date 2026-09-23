using System.Net;
using System.Text;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure;
using Microsoft.Extensions.Options;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class DataForSeoGoogleShoppingMonitorTests
{
    [Fact]
    public async Task ObserveAsync_MapsMatchingSellerPriceAndAvailability()
    {
        var handler = new FakeDataForSeoHandler("""
        {
          "status_code": 20000,
          "tasks": [{
            "id": "product-info-task",
            "status_code": 20000,
            "status_message": "Ok.",
            "result": [{
              "product_id": "12345",
              "type": "product_info",
              "check_url": "https://www.google.co.uk/shopping/product/12345",
              "datetime": "2026-09-23 10:55:00 +00:00",
              "items": [{
                "type": "product_info_element",
                "sellers": [{
                  "type": "product_seller",
                  "title": "Example Retailer",
                  "url": "https://example-retailer.test/product/merries-l",
                  "price": {
                    "current": 12.99,
                    "regular": 14.99,
                    "max_value": 14.99,
                    "currency": "GBP",
                    "displayed_price": "£12.99"
                  },
                  "product_availability": "in_stock"
                }]
              }]
            }]
          }]
        }
        """);

        var monitor = CreateMonitor(handler);
        var listing = CreateListing();

        var reading = await monitor.ObserveAsync(listing);

        Assert.Equal(new DateTimeOffset(2026, 9, 23, 10, 55, 0, TimeSpan.Zero), reading.ObservedAtUtc);
        Assert.Equal(12.99m, reading.PriceAmount);
        Assert.Equal("GBP", reading.PriceCurrencyCode);
        Assert.Equal(RetailerProductAvailability.InStock, reading.Availability);
        Assert.Equal("DataForSEO.GoogleShopping.ProductInfo", reading.Source);
        Assert.Equal("https://www.google.co.uk/shopping/product/12345", reading.SourceUrl);
    }

    [Fact]
    public async Task ObserveAsync_MapsOutOfStock()
    {
        var handler = new FakeDataForSeoHandler("""
        {
          "status_code": 20000,
          "tasks": [{
            "id": "product-info-task",
            "status_code": 20000,
            "status_message": "Ok.",
            "result": [{
              "datetime": "2026-09-23 11:00:00 +00:00",
              "items": [{
                "type": "product_info_element",
                "sellers": [{
                  "url": "https://example-retailer.test/product/merries-l",
                  "price": {
                    "current": 12.99,
                    "currency": "GBP"
                  },
                  "product_availability": "out_of_stock"
                }]
              }]
            }]
          }]
        }
        """);

        var monitor = CreateMonitor(handler);
        var reading = await monitor.ObserveAsync(CreateListing());

        Assert.Equal(RetailerProductAvailability.OutOfStock, reading.Availability);
    }

    [Fact]
    public async Task ObserveAsync_MapsLimitedStockToInStock()
    {
        var handler = new FakeDataForSeoHandler("""
        {
          "status_code": 20000,
          "tasks": [{
            "id": "product-info-task",
            "status_code": 20000,
            "result": [{
              "items": [{
                "type": "product_info_element",
                "sellers": [{
                  "url": "https://example-retailer.test/product/merries-l/",
                  "price": { "current": 9.99, "currency": "GBP" },
                  "product_availability": "limited_stock"
                }]
              }]
            }]
          }]
        }
        """);

        var reading = await CreateMonitor(handler).ObserveAsync(CreateListing());

        Assert.Equal(RetailerProductAvailability.InStock, reading.Availability);
    }

    [Fact]
    public void CanMonitor_RequiresDataForSeoDiscoveryAndExternalProductId()
    {
        var monitor = CreateMonitor(new FakeDataForSeoHandler("{}"));
        var supported = CreateListing();

        Assert.True(monitor.CanMonitor(supported));

        var wrongProvider = new RetailerProductListing(
            supported.PackTypeId,
            supported.RetailerId,
            supported.ListingUrl,
            "Other.Provider",
            externalListingId: "12345");

        Assert.False(monitor.CanMonitor(wrongProvider));

        var withoutProductId = new RetailerProductListing(
            supported.PackTypeId,
            supported.RetailerId,
            supported.ListingUrl,
            "DataForSEO.GoogleShopping");

        Assert.False(monitor.CanMonitor(withoutProductId));
    }

    [Fact]
    public async Task ObserveAsync_ThrowsWhenNoSellerMatchesListingUrl()
    {
        var handler = new FakeDataForSeoHandler("""
        {
          "status_code": 20000,
          "tasks": [{
            "id": "product-info-task",
            "status_code": 20000,
            "result": [{
              "items": [{
                "type": "product_info_element",
                "sellers": [{
                  "url": "https://another-retailer.test/product/merries-l",
                  "price": { "current": 12.99, "currency": "GBP" },
                  "product_availability": "in_stock"
                }]
              }]
            }]
          }]
        }
        """);

        var monitor = CreateMonitor(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => monitor.ObserveAsync(CreateListing()));
    }

    private static DataForSeoGoogleShoppingMonitor CreateMonitor(FakeDataForSeoHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.dataforseo.com/")
        };

        return new DataForSeoGoogleShoppingMonitor(
            client,
            Options.Create(new DataForSeoOptions
            {
                Enabled = true,
                Login = "test-login",
                Password = "test-password",
                LocationName = "London,England,United Kingdom",
                LanguageCode = "en",
                SearchDomain = "google.co.uk",
                PollAttempts = 2,
                PollDelaySeconds = 1
            }));
    }

    private static RetailerProductListing CreateListing() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "https://example-retailer.test/product/merries-l",
            "DataForSEO.GoogleShopping",
            externalListingId: "12345");

    private sealed class FakeDataForSeoHandler(string completedResponse) : HttpMessageHandler
    {
        private readonly string completedResponse = completedResponse;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            var response = path switch
            {
                "/v3/merchant/google/product_info/task_post" => Json("""
                    {"status_code":20000,"tasks":[{"id":"product-info-task","status_code":20100,"status_message":"Task Created.","result":null}]}
                    """),
                "/v3/merchant/google/product_info/task_get/advanced/product-info-task" => Json(completedResponse),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            };

            return Task.FromResult(response);
        }

        private static HttpResponseMessage Json(string content) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };
    }
}
