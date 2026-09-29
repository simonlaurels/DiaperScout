namespace DiaperScout.Domain;

public sealed class PasskeyCredential : Entity
{
    private PasskeyCredential() { }

    public PasskeyCredential(Guid userId, byte[] credentialId, byte[] publicKey, long signCount,
        bool backupEligible, bool backedUp, string name, DateTimeOffset createdAtUtc)
    {
        UserId = userId;
        CredentialId = credentialId;
        PublicKey = publicKey;
        SignCount = signCount;
        BackupEligible = backupEligible;
        BackedUp = backedUp;
        Name = name;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid UserId { get; private set; }
    public byte[] CredentialId { get; private set; } = [];
    public byte[] PublicKey { get; private set; } = [];
    public long SignCount { get; private set; }
    public bool BackupEligible { get; private set; }
    public bool BackedUp { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? LastUsedAtUtc { get; private set; }
    public Guid Version { get; private set; } = Guid.NewGuid();
}

public enum PasskeyCeremony { Registration, SignIn }

public sealed class PasskeyChallenge : Entity
{
    private PasskeyChallenge() { }

    public PasskeyChallenge(Guid? userId, PasskeyCeremony ceremony, string bindingHash,
        string optionsJson, DateTimeOffset expiresAtUtc)
    {
        UserId = userId;
        Ceremony = ceremony;
        BindingHash = bindingHash;
        OptionsJson = optionsJson;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid? UserId { get; private set; }
    public PasskeyCeremony Ceremony { get; private set; }
    public string BindingHash { get; private set; } = string.Empty;
    public string OptionsJson { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? UsedAtUtc { get; private set; }
}
