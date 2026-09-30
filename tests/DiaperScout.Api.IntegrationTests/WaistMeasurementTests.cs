extern alias DiaperScoutWeb;

using DiaperScoutWeb::DiaperScout.Web.Services;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class WaistMeasurementTests
{
    [Theory]
    [InlineData(30, 76)]
    [InlineData(32.5, 83)]
    [InlineData(35, 89)]
    public void Inches_ConvertToNearestStoredCentimetre(double inches, int expected)
    {
        var editor = new WaistMeasurementEditor { WaistUnit = WaistMeasurementUnit.Inches, WaistMinimum = (decimal)inches };
        Assert.Null(editor.ValidateWaist());
        Assert.Equal(expected, editor.WaistMinimumCm);
        Assert.Null(editor.WaistMaximumCm);
    }

    [Fact]
    public void RepeatedUnitChanges_PreserveExistingCentimetresAndUnknownBounds()
    {
        var editor = new WaistMeasurementEditor { WaistMinimumCm = 90 };
        for (var i = 0; i < 10; i++)
        {
            editor.WaistUnit = WaistMeasurementUnit.Inches;
            Assert.Equal(35.43m, editor.WaistMinimum);
            Assert.Null(editor.WaistMaximum);
            editor.WaistUnit = WaistMeasurementUnit.Centimetres;
            Assert.Equal(90m, editor.WaistMinimum);
        }
        Assert.Equal(90, editor.WaistMinimumCm);
    }

    [Fact]
    public void UnitChange_ConvertsEnteredRangeWithoutChangingItsMeaning()
    {
        var editor = new WaistMeasurementEditor { WaistUnit = WaistMeasurementUnit.Inches, WaistMinimum = 32.5m, WaistMaximum = 40m };
        editor.WaistUnit = WaistMeasurementUnit.Centimetres;
        Assert.Equal(82.55m, editor.WaistMinimum);
        Assert.Equal(101.6m, editor.WaistMaximum);
        Assert.Equal(83, editor.WaistMinimumCm);
        Assert.Equal(102, editor.WaistMaximumCm);
        editor.WaistUnit = WaistMeasurementUnit.Inches;
        Assert.Equal(32.5m, editor.WaistMinimum);
        Assert.Equal(40m, editor.WaistMaximum);
    }

    [Fact]
    public void Validation_RejectsInvalidRangesBeforeRoundingCanHideThem()
    {
        var editor = new WaistMeasurementEditor { WaistUnit = WaistMeasurementUnit.Inches, WaistMinimum = 30.1m, WaistMaximum = 30m };
        Assert.Contains("minimum", editor.ValidateWaist()!);
        editor.WaistMinimum = -0.1m;
        Assert.Contains("negative", editor.ValidateWaist()!);
        editor.WaistMinimum = int.MaxValue;
        Assert.Contains("too large", editor.ValidateWaist()!);
    }

    [Theory]
    [InlineData(76, 102, "76–102 cm", "in)")]
    [InlineData(76, null, "76+ cm", "in)")]
    [InlineData(null, 102, "Up to 102 cm", "in)")]
    [InlineData(null, null, "Size recorded", "Size recorded")]
    public void ProductRange_ShowsBothUnitsWhenKnown(int? minimum, int? maximum, string centimetres, string inches)
    {
        var range = WaistMeasurementFormatting.FormatRange(minimum, maximum);
        Assert.Contains(centimetres, range);
        Assert.Contains(inches, range);
    }
}
