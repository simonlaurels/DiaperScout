using System.Buffers.Binary;
using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DiaperScout.Application;

namespace DiaperScout.Api.IntegrationTests;

// A synthetic authenticator: the server still verifies real P-256 signatures and WebAuthn data.
internal sealed class TestPasskey : IDisposable
{
    private readonly ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    public byte[] CredentialId { get; } = RandomNumberGenerator.GetBytes(32);
    public byte[] UserHandle { get; private set; } = [];

    public JsonElement Register(PasskeyOptions options, string origin = "https://localhost:7167", bool verified = true, bool backupEligible = false)
    {
        UserHandle = Decode(options.PublicKey.GetProperty("user").GetProperty("id").GetString()!);
        var parameters = key.ExportParameters(false);
        var cose = new CborWriter();
        cose.WriteStartMap(5);
        cose.WriteInt32(1); cose.WriteInt32(2);
        cose.WriteInt32(3); cose.WriteInt32(-7);
        cose.WriteInt32(-1); cose.WriteInt32(1);
        cose.WriteInt32(-2); cose.WriteByteString(parameters.Q.X!);
        cose.WriteInt32(-3); cose.WriteByteString(parameters.Q.Y!);
        cose.WriteEndMap();
        var authData = AuthData("localhost", (byte)((verified ? 0x45 : 0x41) | (backupEligible ? 8 : 0)), 0).Concat(new byte[16]).ToList();
        var length = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(length, (ushort)CredentialId.Length);
        authData.AddRange(length);
        authData.AddRange(CredentialId);
        authData.AddRange(cose.Encode());
        var attestation = new CborWriter();
        attestation.WriteStartMap(3);
        attestation.WriteTextString("fmt"); attestation.WriteTextString("none");
        attestation.WriteTextString("attStmt"); attestation.WriteStartMap(0); attestation.WriteEndMap();
        attestation.WriteTextString("authData"); attestation.WriteByteString(authData.ToArray());
        attestation.WriteEndMap();
        return JsonSerializer.SerializeToElement(new
        {
            id = Encode(CredentialId), rawId = Encode(CredentialId), type = "public-key", clientExtensionResults = new { },
            response = new { clientDataJSON = Encode(ClientData(options, "webauthn.create", origin)),
                attestationObject = Encode(attestation.Encode()), transports = new[] { "internal" } }
        });
    }

    public JsonElement Assert(PasskeyOptions options, uint counter = 1, string origin = "https://localhost:7167",
        string rpId = "localhost", byte flags = 5, bool badSignature = false, bool crossOrigin = false, byte[]? userHandle = null)
    {
        var clientData = ClientData(options, "webauthn.get", origin, crossOrigin);
        var authData = AuthData(rpId, flags, counter);
        var signed = authData.Concat(SHA256.HashData(clientData)).ToArray();
        var signature = key.SignData(signed, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        if (badSignature) signature[^1] ^= 1;
        return JsonSerializer.SerializeToElement(new
        {
            id = Encode(CredentialId), rawId = Encode(CredentialId), type = "public-key", clientExtensionResults = new { },
            response = new { clientDataJSON = Encode(clientData), authenticatorData = Encode(authData),
                signature = Encode(signature), userHandle = Encode(userHandle ?? UserHandle) }
        });
    }

    private static byte[] AuthData(string rpId, byte flags, uint counter)
    {
        var data = new byte[37];
        SHA256.HashData(Encoding.UTF8.GetBytes(rpId)).CopyTo(data, 0);
        data[32] = flags;
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(33), counter);
        return data;
    }
    private static byte[] ClientData(PasskeyOptions options, string type, string origin, bool crossOrigin = false) =>
        JsonSerializer.SerializeToUtf8Bytes(new { type, challenge = options.PublicKey.GetProperty("challenge").GetString(), origin, crossOrigin });
    internal static string Encode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    internal static byte[] Decode(string value) => Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/').PadRight((value.Length + 3) / 4 * 4, '='));
    public void Dispose() => key.Dispose();
}
