# PatchTool

Signing for retail patch downloads.

ffxivboot only downloads a patch torrent whose `X-Signature` checks out against the certificate in
its `BINARY` resource 111. That is Square Enix's certificate, and its private key is gone. This
tool makes our own key and certificate and signs the retail torrents with it.
[RetailRedirect](../RetailRedirect/README.md) hands the certificate to boot in place of resource 111
(`cert=` under `[Boot]` in `iphlpapi.ini`), so `ffxivboot.exe` stays retail. The torrents stay
exactly as retail shipped them too.

The signature: RSA PKCS#1 v1.5 with an MD5 DigestInfo over `MD5(SHA1(torrent file))`, sent as
URL-safe base64. `verify` against the retail boot accepts the one surviving retail signature
(`D2012.09.19.0001.torrent`), so the format is boot's exactly.

## Use

From the repository root (`pt` = `dotnet run --project Tools/PatchTool --`):

```
pt keygen --out <secrets dir>
pt sign --key <secrets dir>/patch_key.pem --torrents <dir with <repo id>/metainfo/*.torrent> --out signatures.json
pt verify --cert <secrets dir>/patch_cert.der --torrents <dir> --signatures signatures.json
pt extract-cert --exe "<install>/ffxivboot.exe" --out retail_cert.der
```

`keygen` makes `patch_key.pem` (private), `patch_cert.der` and `patch_cert.pem`: X.509 v3,
RSA 1024, sha1WithRSA, like the retail certificate. Players get `patch_cert.der` with
RetailRedirect.

## The key

- `patch_key.pem` signs everything players' boot will accept. Keep it off git and off shared
  drives; the server reads it to sign version-check replies.
- A new key means a new `patch_cert.der` for every player. `keygen` refuses to overwrite an
  existing key.
