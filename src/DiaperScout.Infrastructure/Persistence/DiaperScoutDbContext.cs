using DiaperScout.Domain;
using Microsoft.EntityFrameworkCore;

namespace DiaperScout.Infrastructure.Persistence;

public sealed class DiaperScoutDbContext(DbContextOptions<DiaperScoutDbContext> options) : DbContext(options)
{
    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<SizeVariant> SizeVariants => Set<SizeVariant>();
    public DbSet<PackType> PackTypes => Set<PackType>();
    public DbSet<ProductIdentifier> ProductIdentifiers => Set<ProductIdentifier>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Retailer> Retailers => Set<Retailer>();
    public DbSet<RetailerProductListing> RetailerProductListings => Set<RetailerProductListing>();
    public DbSet<RetailerIdentityVerification> RetailerIdentityVerifications => Set<RetailerIdentityVerification>();
    public DbSet<RetailerAffiliateProgramme> RetailerAffiliateProgrammes => Set<RetailerAffiliateProgramme>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<User> Users => Set<User>();
    public DbSet<PrivilegedRoleAssignment> PrivilegedRoleAssignments => Set<PrivilegedRoleAssignment>();
    public DbSet<PrivilegedRoleAssignmentAudit> PrivilegedRoleAssignmentAudits => Set<PrivilegedRoleAssignmentAudit>();
    public DbSet<CatalogueAuditRecord> CatalogueAuditRecords => Set<CatalogueAuditRecord>();
    public DbSet<CatalogueSubmission> CatalogueSubmissions => Set<CatalogueSubmission>();
    public DbSet<CatalogueSubmissionVariant> CatalogueSubmissionVariants => Set<CatalogueSubmissionVariant>();
    public DbSet<CatalogueSubmissionSizeVariant> CatalogueSubmissionSizeVariants => Set<CatalogueSubmissionSizeVariant>();
    public DbSet<CatalogueSubmissionVariantOverride> CatalogueSubmissionVariantOverrides => Set<CatalogueSubmissionVariantOverride>();
    public DbSet<CatalogueSubmissionVerification> CatalogueSubmissionVerifications => Set<CatalogueSubmissionVerification>();
    public DbSet<CatalogueSubmissionRetailDestination> CatalogueSubmissionRetailDestinations => Set<CatalogueSubmissionRetailDestination>();
    public DbSet<CatalogueSubmissionRetailAffiliate> CatalogueSubmissionRetailAffiliates => Set<CatalogueSubmissionRetailAffiliate>();
    public DbSet<CatalogueSubmissionEditorialDecision> CatalogueSubmissionEditorialDecisions => Set<CatalogueSubmissionEditorialDecision>();
    public DbSet<CatalogueSubmissionImage> CatalogueSubmissionImages => Set<CatalogueSubmissionImage>();
    public DbSet<ExplorerProfile> ExplorerProfiles => Set<ExplorerProfile>();
    public DbSet<Backpack> Backpacks => Set<Backpack>();
    public DbSet<SavedProduct> SavedProducts => Set<SavedProduct>();
    public DbSet<SavedLocation> SavedLocations => Set<SavedLocation>();
    public DbSet<Observation> Observations => Set<Observation>();
    public DbSet<RetailerProductObservation> RetailerProductObservations => Set<RetailerProductObservation>();
    public DbSet<EvidenceItem> EvidenceItems => Set<EvidenceItem>();
    public DbSet<EditorialDecision> EditorialDecisions => Set<EditorialDecision>();
    public DbSet<KnowledgeGap> KnowledgeGaps => Set<KnowledgeGap>();
    public DbSet<DiscoveryTask> DiscoveryTasks => Set<DiscoveryTask>();
    public DbSet<DataForSeoIntegrationSetting> DataForSeoIntegrationSettings => Set<DataForSeoIntegrationSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("diaperscout");

        foreach (var entity in modelBuilder.Model.GetEntityTypes().Where(entity => typeof(Entity).IsAssignableFrom(entity.ClrType)))
        {
            modelBuilder.Entity(entity.ClrType).HasKey(nameof(Entity.Id));
        }

        modelBuilder.Entity<Manufacturer>(entity =>
        {
            entity.ToTable("manufacturers");
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.ToTable("brands");
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasOne<Manufacturer>()
                .WithMany(x => x.Brands)
                .HasForeignKey(x => x.ManufacturerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.Property(x => x.Name).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Description).HasColumnType("text");
            entity.Property(x => x.DescriptionVisibility).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => new { x.ManufacturerId, x.Name });
            entity.HasOne<Manufacturer>()
                .WithMany()
                .HasForeignKey(x => x.ManufacturerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Brand>()
                .WithMany()
                .HasForeignKey(x => x.BrandId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("product_variants");
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.BackingType).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.FastenerType).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.WaistbandStyle).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Fragrance).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.PrintDesign).HasMaxLength(500);
            entity.Property(x => x.PrimaryColour).HasMaxLength(100);
            entity.Property(x => x.DesignedFor).HasMaxLength(100);
            entity.Property(x => x.ConstructionNotes).HasColumnType("text");
            entity.HasIndex(x => new { x.ProductId, x.Name }).IsUnique();
            entity.HasOne<Product>()
                .WithMany(x => x.Variants)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SizeVariant>(entity =>
        {
            entity.ToTable(
                "size_variants",
                table => table.HasCheckConstraint(
                    "CK_size_variants_waist_range",
                    "\"WaistMinimumCm\" IS NULL OR \"WaistMaximumCm\" IS NULL OR \"WaistMinimumCm\" <= \"WaistMaximumCm\""));

            entity.Property(x => x.ManufacturerSize).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => new { x.ProductVariantId, x.ManufacturerSize }).IsUnique();
            entity.HasOne<ProductVariant>()
                .WithMany(x => x.Sizes)
                .HasForeignKey(x => x.ProductVariantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PackType>(entity =>
        {
            entity.ToTable(
                "pack_types",
                table => table.HasCheckConstraint(
                    "CK_pack_types_quantity",
                    "\"QuantityPerPack\" > 0"));

            entity.HasIndex(x => new { x.SizeVariantId, x.QuantityPerPack, x.PackagingType }).IsUnique();
            entity.HasOne<SizeVariant>()
                .WithMany(x => x.PackTypes)
                .HasForeignKey(x => x.SizeVariantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductIdentifier>(entity =>
        {
            entity.ToTable("product_identifiers");
            entity.Property(x => x.Value).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => new { x.Type, x.Value }).IsUnique();
            entity.HasOne<PackType>()
                .WithMany(x => x.Identifiers)
                .HasForeignKey(x => x.PackTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Country>(entity =>
        {
            entity.ToTable("countries");
            entity.Property(x => x.IsoCode).HasMaxLength(2).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.IsoCode).IsUnique();
        });

        modelBuilder.Entity<Retailer>(entity =>
        {
            entity.ToTable("retailers");
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IdentitySourceUrl).HasMaxLength(2048);
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<RetailerIdentityVerification>(entity =>
        {
            entity.ToTable("retailer_identity_verifications");
            entity.Property(x => x.Outcome).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.ObservedRetailerName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.SourceUrl).HasMaxLength(2048).IsRequired();
            entity.Property(x => x.ListingUrl).HasMaxLength(2048);
            entity.Property(x => x.Notes).HasColumnType("text");
            entity.Property(x => x.VerifiedAtUtc).IsRequired();
            entity.HasIndex(x => new { x.RetailerId, x.VerifiedAtUtc });
            entity.HasOne<Retailer>()
                .WithMany()
                .HasForeignKey(x => x.RetailerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.VerifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RetailerProductObservation>(entity =>
        {
            entity.ToTable("retailer_product_observations");
            entity.Property(x => x.PriceAmount).HasPrecision(12, 2);
            entity.Property(x => x.PriceCurrencyCode).HasMaxLength(3);
            entity.Property(x => x.Availability).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(x => x.Source).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SourceUrl).HasMaxLength(2000);
            entity.HasIndex(x => new { x.RetailerProductListingId, x.ObservedAtUtc });
            entity.HasOne<RetailerProductListing>()
                .WithMany()
                .HasForeignKey(x => x.RetailerProductListingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RetailerProductListing>(entity =>
        {
            entity.ToTable("retailer_product_listings");
            entity.Property(x => x.ListingUrl).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.DiscoveryProvider).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SourceUrl).HasMaxLength(2000);
            entity.Property(x => x.ExternalListingId).HasMaxLength(300);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.HasIndex(x => new { x.PackTypeId, x.RetailerId, x.ListingUrl }).IsUnique();
            entity.HasIndex(x => new { x.RetailerId, x.Status });
            entity.HasOne<PackType>()
                .WithMany()
                .HasForeignKey(x => x.PackTypeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Retailer>()
                .WithMany()
                .HasForeignKey(x => x.RetailerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RetailerAffiliateProgramme>(entity =>
        {
            entity.ToTable("retailer_affiliate_programmes");
            entity.Property(x => x.Network).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ProgrammeId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ProgrammeName).HasMaxLength(300).IsRequired();
            entity.Property(x => x.ProgrammeUrl).HasMaxLength(2048);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.TermsUrl).HasMaxLength(2048);
            entity.Property(x => x.ReferralTerms).HasColumnType("text");
            entity.Property(x => x.SourceUrl).HasMaxLength(2048);
            entity.Property(x => x.DiscoveredAtUtc).IsRequired();
            entity.Property(x => x.LastCheckedAtUtc).IsRequired();
            entity.Property(x => x.IsPreferred).IsRequired();
            entity.HasIndex(x => new { x.RetailerId, x.Network, x.ProgrammeId }).IsUnique();
            entity.HasIndex(x => new { x.RetailerId, x.IsPreferred });
            entity.HasOne<Retailer>()
                .WithMany()
                .HasForeignKey(x => x.RetailerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CatalogueSubmissionRetailDestination>(entity =>
        {
            entity.ToTable("catalogue_submission_retail_destinations");

            entity.Property(x => x.ListingUrl)
                .HasMaxLength(2048)
                .IsRequired();

            entity.Property(x => x.Notes)
                .HasColumnType("text");

            entity.HasIndex(x => new { x.SubmissionId, x.RetailerId });

            entity.HasOne<CatalogueSubmission>()
                .WithMany()
                .HasForeignKey(x => x.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Retailer>()
                .WithMany()
                .HasForeignKey(x => x.RetailerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CatalogueSubmissionRetailAffiliate>(entity =>
        {
            entity.ToTable("catalogue_submission_retail_affiliates");

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(x => x.Network)
                .HasMaxLength(200);

            entity.Property(x => x.TrackingConfiguration)
                .HasColumnType("text");

            entity.Property(x => x.DeepLinkMechanism)
                .HasMaxLength(500);

            entity.Property(x => x.TermsUrl)
                .HasMaxLength(2048);

            entity.Property(x => x.ApplicationReference)
                .HasMaxLength(500);

            entity.Property(x => x.Notes)
                .HasColumnType("text");

            entity.HasIndex(x => new { x.SubmissionId, x.RetailDestinationId })
                .IsUnique();

            entity.HasIndex(x => new { x.RetailDestinationId, x.LastVerifiedAtUtc });

            entity.HasOne<CatalogueSubmission>()
                .WithMany()
                .HasForeignKey(x => x.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<CatalogueSubmissionRetailDestination>()
                .WithMany()
                .HasForeignKey(x => x.RetailDestinationId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("locations");
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.AddressLine1).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Locality).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Postcode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Latitude).HasPrecision(9, 6);
            entity.Property(x => x.Longitude).HasPrecision(9, 6);
            entity.HasIndex(x => new { x.RetailerId, x.Postcode, x.Name }).IsUnique();
            entity.HasOne<Retailer>()
                .WithMany()
                .HasForeignKey(x => x.RetailerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Country>()
                .WithMany()
                .HasForeignKey(x => x.CountryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.Property(x => x.Subject).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.Subject).IsUnique();
            entity.HasOne(x => x.ExplorerProfile)
                .WithOne()
                .HasForeignKey<ExplorerProfile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PrivilegedRoleAssignment>(entity =>
        {
            entity.ToTable("privileged_role_assignments");
            entity.HasIndex(x => new { x.UserId, x.Role })
                .IsUnique()
                .HasFilter("\"RevokedAtUtc\" IS NULL");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.GrantedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.RevokedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PrivilegedRoleAssignmentAudit>(entity =>
        {
            entity.ToTable("privileged_role_assignment_audits");
            entity.HasIndex(x => new { x.SubjectUserId, x.OccurredAtUtc });

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.ActingUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.SubjectUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CatalogueAuditRecord>(entity =>
        {
            entity.ToTable("catalogue_audit_records");
            entity.Property(x => x.SubmittedPayloadJson).HasColumnType("text").IsRequired();
            entity.Property(x => x.AffectedCanonicalIdsJson).HasColumnType("text").IsRequired();
            entity.Property(x => x.SourceSummary).HasColumnType("text").IsRequired();
            entity.Property(x => x.SourceReferencesJson).HasColumnType("text").IsRequired();
            entity.Property(x => x.EditorialRationale).HasColumnType("text").IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(200);
            entity.HasIndex(x => new { x.ProductId, x.OccurredAtUtc });

            entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.ActingUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CatalogueSubmission>(entity =>
        {
            entity.ToTable("catalogue_submissions");

            entity.Property(x => x.ProposedManufacturerName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.ProposedBrandName)
                .HasMaxLength(200);

            entity.Property(x => x.ProposedProductName)
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(x => x.ProposedVariantName)
                .HasMaxLength(200);

            entity.Property(x => x.ProposedGtin)
                .HasMaxLength(14);

            entity.Property(x => x.ProposedSku)
                .HasMaxLength(200);

            entity.Property(x => x.IdentitySourceUrl)
                .HasMaxLength(2048);

            entity.Property(x => x.ProposedProductType)
                .HasConversion<string>()
                .HasMaxLength(32);

            entity.Property(x => x.ProposedDescriptionVisibility)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(x => x.ProposedPackagingType)
                .HasConversion<string>()
                .HasMaxLength(32);

            entity.Property(x => x.SharedWaistbandStyle)
                .HasConversion<string>()
                .HasMaxLength(32);

            entity.Property(x => x.SharedFragrance)
                .HasConversion<string>()
                .HasMaxLength(32);

            entity.Property(x => x.SharedDesignedFor)
                .HasMaxLength(100);

            entity.Property(x => x.Notes)
                .HasColumnType("text");

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(x => x.Source)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAtUtc);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.SubmittedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(x => x.PublishedProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CatalogueSubmissionVariant>(entity =>
        {
            entity.ToTable("catalogue_submission_variants");

            entity.Property(x => x.Name)
                .HasMaxLength(200);

            entity.HasIndex(x => new { x.SubmissionId, x.Name })
                .IsUnique();

            entity.HasIndex(x => x.SubmissionId)
                .IsUnique()
                .HasFilter("\"Name\" IS NULL");

            entity.HasOne<CatalogueSubmission>()
                .WithMany()
                .HasForeignKey(x => x.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CatalogueSubmissionSizeVariant>(entity =>
        {
            entity.ToTable("catalogue_submission_size_variants");

            entity.Property(x => x.ManufacturerSize)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.FitMeasurementBasis).HasMaxLength(200);
            entity.Property(x => x.AbsorbencyBasisMethod).HasMaxLength(500);
            entity.Property(x => x.AbsorbencySource).HasMaxLength(2048);

            entity.Property(x => x.Gtin)
                .HasMaxLength(14);

            entity.HasIndex(x => new { x.VariantId, x.ManufacturerSize })
                .IsUnique();

            entity.HasIndex(x => x.VariantId);

            entity.HasOne<CatalogueSubmissionVariant>()
                .WithMany(x => x.Sizes)
                .HasForeignKey(x => x.VariantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CatalogueSubmissionVariantOverride>(entity =>
        {
            entity.ToTable("catalogue_submission_variant_overrides");

            entity.Property(x => x.BackingType).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.FastenerType).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.WaistbandStyle).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Fragrance).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.PrintDesign).HasMaxLength(500);
            entity.Property(x => x.PrimaryColour).HasMaxLength(100);
            entity.Property(x => x.DesignedFor).HasMaxLength(100);
            entity.Property(x => x.ConstructionNotes).HasColumnType("text");

            entity.HasIndex(x => x.VariantId).IsUnique();
            entity.HasOne<CatalogueSubmissionVariant>()
                .WithMany()
                .HasForeignKey(x => x.VariantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CatalogueSubmissionVerification>(entity =>
        {
            entity.ToTable("catalogue_submission_verifications");

            entity.Property(x => x.Area)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(x => x.Scope)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.Source)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.SourceUrl)
                .HasMaxLength(2048);

            entity.Property(x => x.Notes)
                .HasColumnType("text");

            entity.Property(x => x.PermissionTerms)
                .HasColumnType("text");

            entity.HasIndex(x => new { x.SubmissionId, x.Area, x.VerifiedAtUtc });

            entity.HasOne<CatalogueSubmission>()
                .WithMany()
                .HasForeignKey(x => x.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.VerifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CatalogueSubmissionEditorialDecision>(entity =>
        {
            entity.ToTable("catalogue_submission_editorial_decisions");

            entity.Property(x => x.Outcome)
                .HasConversion<string>()
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(x => x.Rationale)
                .HasColumnType("text");

            entity.HasIndex(x => new { x.SubmissionId, x.DecidedAtUtc });

            entity.HasOne<CatalogueSubmission>()
                .WithMany()
                .HasForeignKey(x => x.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.ModeratorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CatalogueSubmissionImage>(entity =>
        {
            entity.ToTable("catalogue_submission_images");
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
            entity.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(x => x.SourceUrl).HasMaxLength(2000);
            entity.Property(x => x.SourceNotes).HasColumnType("text");
            entity.Property(x => x.PermissionStatus).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(x => x.PermissionEvidence).HasColumnType("text");
            entity.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.IsPrimary).IsRequired();
            entity.HasIndex(x => x.ProductId)
                .IsUnique()
                .HasFilter("\"ProductId\" IS NOT NULL AND \"IsPrimary\" = TRUE")
                .HasDatabaseName("IX_catalogue_submission_images_ProductId_IsPrimary");
            entity.HasIndex(x => new { x.ProductId, x.Role })
                .IsUnique()
                .HasFilter("\"ProductId\" IS NOT NULL AND \"Role\" <> 'Other'");
            entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.SubmissionId, x.Role })
                .IsUnique()
                .HasFilter("\"Role\" <> 'Other'");
            entity.HasOne<CatalogueSubmission>()
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExplorerProfile>(entity =>
        {
            entity.ToTable("explorer_profiles");
            entity.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.DisplayName).IsUnique();
            entity.HasOne(x => x.Backpack)
                .WithOne()
                .HasForeignKey<Backpack>(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Backpack>(entity =>
        {
            entity.ToTable("backpacks");
            entity.HasIndex(x => x.UserId).IsUnique();
        });

        modelBuilder.Entity<SavedProduct>(entity =>
        {
            entity.ToTable("saved_products");
            entity.HasIndex(x => new { x.BackpackId, x.ProductId }).IsUnique();
            entity.HasOne<Backpack>()
                .WithMany(x => x.SavedProducts)
                .HasForeignKey(x => x.BackpackId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SavedLocation>(entity =>
        {
            entity.ToTable("saved_locations");
            entity.HasIndex(x => new { x.BackpackId, x.LocationId }).IsUnique();
            entity.HasOne<Backpack>()
                .WithMany(x => x.SavedLocations)
                .HasForeignKey(x => x.BackpackId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Location>()
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Observation>(entity =>
        {
            entity.ToTable(
                "observations",
                table => table.HasCheckConstraint(
                    "CK_observations_subject",
                    "\"ProductId\" IS NOT NULL OR \"CandidateProductName\" IS NOT NULL"));

            entity.Property(x => x.Narrative).HasColumnType("text");
            entity.Property(x => x.CandidateProductName).HasMaxLength(250);
            entity.Property(x => x.PriceAmount).HasPrecision(12, 2);
            entity.Property(x => x.PriceCurrencyCode).HasMaxLength(3);
            entity.HasIndex(x => new { x.ProductId, x.ObservedAtUtc });
            entity.HasIndex(x => new { x.LocationId, x.ObservedAtUtc });
            entity.HasIndex(x => new { x.AuthorUserId, x.CreatedAtUtc });

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Location>()
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EvidenceItem>(entity =>
        {
            entity.ToTable("evidence_items");
            entity.Property(x => x.StorageKey).HasMaxLength(1000).IsRequired();
            entity.HasIndex(x => new { x.ObservationId, x.SubmittedAtUtc });

            entity.HasOne<Observation>()
                .WithMany(x => x.Evidence)
                .HasForeignKey(x => x.ObservationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.SubmittedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EditorialDecision>(entity =>
        {
            entity.ToTable("editorial_decisions");
            entity.Property(x => x.Rationale).HasColumnType("text");
            entity.HasIndex(x => new { x.ObservationId, x.DecidedAtUtc });

            entity.HasOne<Observation>()
                .WithMany()
                .HasForeignKey(x => x.ObservationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.ModeratorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KnowledgeGap>(entity =>
        {
            entity.ToTable("knowledge_gaps");
            entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Description).HasColumnType("text");
            entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Location>()
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DiscoveryTask>(entity =>
        {
            entity.ToTable("discovery_tasks");
            entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Description).HasColumnType("text");
            entity.HasIndex(x => x.State);

            entity.HasOne<KnowledgeGap>()
                .WithMany()
                .HasForeignKey(x => x.KnowledgeGapId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.AcceptedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Observation>()
                .WithMany()
                .HasForeignKey(x => x.ResultingObservationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<DataForSeoIntegrationSetting>(entity =>
        {
            entity.ToTable("dataforseo_integration_settings");
            entity.Property(x => x.ProviderKey).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Login).HasMaxLength(320).IsRequired();
            entity.Property(x => x.ProtectedPassword).HasColumnType("text");
            entity.Property(x => x.LocationName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.LanguageCode).HasMaxLength(20).IsRequired();
            entity.Property(x => x.SearchDomain).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.HasIndex(x => x.ProviderKey).IsUnique();
        });

    }
}