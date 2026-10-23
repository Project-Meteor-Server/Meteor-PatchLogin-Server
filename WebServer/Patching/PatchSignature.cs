using System;
using System.Security.Cryptography;

namespace Meteor.Web.Patching;

// The signature ffxivboot checks on each torrent it is offered (the X-Signature header).
// Boot hashes the torrent with SHA-1, then checks that hash against an MD5-with-RSA signature
// using the certificate built into ffxivboot. Retail sent it as URL-safe base64, with padding.
public static class PatchSignature
{
    public static string Sign(RSA key, byte[] torrent)
    {
        byte[] signature = key.SignData(SHA1.HashData(torrent), HashAlgorithmName.MD5, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signature).Replace('+', '-').Replace('/', '_');
    }

    public static bool Verify(RSA publicKey, byte[] torrent, string signature)
    {
        byte[] raw;
        try
        {
            raw = Convert.FromBase64String(signature.Trim().Replace('-', '+').Replace('_', '/'));
        }
        catch (FormatException)
        {
            return false;
        }
        return publicKey.VerifyData(SHA1.HashData(torrent), raw, HashAlgorithmName.MD5, RSASignaturePadding.Pkcs1);
    }
}
