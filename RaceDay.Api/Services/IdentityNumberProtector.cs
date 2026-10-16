using System.Security.Cryptography;
using System.Text;

namespace RaceDay.Api.Services;

public sealed class IdentityNumberProtector
{
    private const string VersionPrefix = "v1:";
    private const int NonceLength = 12;
    private const int TagLength = 16;
    private readonly byte[] _key;

    public IdentityNumberProtector(IConfiguration configuration)
        : this(ParseKey(configuration["Identity:EncryptionKey"]))
    {
    }

    public IdentityNumberProtector(byte[] key)
    {
        if (key.Length != 32)
        {
            throw new InvalidOperationException("Identity:EncryptionKey must decode to exactly 32 bytes.");
        }

        _key = key.ToArray();
    }

    public string? Protect(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var nonce = RandomNumberGenerator.GetBytes(NonceLength);
        var plaintext = Encoding.UTF8.GetBytes(value);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagLength];
        using var aes = new AesGcm(_key, TagLength);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var payload = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, payload, nonce.Length + tag.Length, ciphertext.Length);
        return VersionPrefix + Convert.ToBase64String(payload);
    }

    public string? Unprotect(string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (!value.StartsWith(VersionPrefix, StringComparison.Ordinal))
        {
            throw new CryptographicException("Stored identity data is not encrypted with the expected format.");
        }

        var payload = Convert.FromBase64String(value[VersionPrefix.Length..]);
        if (payload.Length < NonceLength + TagLength)
        {
            throw new CryptographicException("Stored identity data is truncated.");
        }

        var nonce = payload[..NonceLength];
        var tag = payload[NonceLength..(NonceLength + TagLength)];
        var ciphertext = payload[(NonceLength + TagLength)..];
        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(_key, TagLength);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }

    private static byte[] ParseKey(string? encodedKey)
    {
        if (string.IsNullOrWhiteSpace(encodedKey))
        {
            throw new InvalidOperationException("Configure Identity:EncryptionKey as a base64-encoded 32-byte key.");
        }

        try
        {
            return Convert.FromBase64String(encodedKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("Identity:EncryptionKey must be valid base64.", exception);
        }
    }
}
