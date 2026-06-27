# GameVault manifest signing

`downloads.json` remains readable JSON. Its separate `downloads.json.sig` file proves that the manifest was signed by the holder of the GameVault private RSA key. The ZIP SHA-256 value inside the manifest still verifies the downloaded game archive; AES-GCM remains reserved for encrypted `.gvb` backups.

## One-time key setup

Build the signer and generate a 3072-bit RSA key pair:

```powershell
dotnet build src/GameVault.ManifestSigner/GameVault.ManifestSigner.csproj -c Release
src/GameVault.ManifestSigner/bin/Release/net10.0/GameVault.ManifestSigner.exe generate-keys --private private_key.pem --public public_key.pem
```

Keep `private_key.pem` only with the developer or on the deployment server. Copy `public_key.pem` to `src/GameVault.Desktop/public_key.pem`, then rebuild Desktop so it is copied to the output directory. The checked-in public key is only a development placeholder and must be replaced with the public key that matches the deployment private key.

Linux example:

```bash
dotnet src/GameVault.ManifestSigner/bin/Release/net10.0/GameVault.ManifestSigner.dll generate-keys --private private_key.pem --public public_key.pem
```

## Sign and verify the server manifest

Place the manifest at `data/static/manifests/downloads.json`, then run:

```powershell
src/GameVault.ManifestSigner/bin/Release/net10.0/GameVault.ManifestSigner.exe sign --manifest data/static/manifests/downloads.json --private private_key.pem --signature data/static/manifests/downloads.json.sig
src/GameVault.ManifestSigner/bin/Release/net10.0/GameVault.ManifestSigner.exe verify --manifest data/static/manifests/downloads.json --public public_key.pem --signature data/static/manifests/downloads.json.sig
```

Linux uses the same arguments through the DLL:

```bash
dotnet src/GameVault.ManifestSigner/bin/Release/net10.0/GameVault.ManifestSigner.dll sign --manifest data/static/manifests/downloads.json --private private_key.pem --signature data/static/manifests/downloads.json.sig
dotnet src/GameVault.ManifestSigner/bin/Release/net10.0/GameVault.ManifestSigner.dll verify --manifest data/static/manifests/downloads.json --public public_key.pem --signature data/static/manifests/downloads.json.sig
```

The static server must expose both URLs:

```text
http://SERVER/files/manifests/downloads.json
http://SERVER/files/manifests/downloads.json.sig
```

Desktop derives the signature URL by appending `.sig` to `DownloadManifestUrl`. Any manifest change requires a new signature. A missing or invalid signature blocks catalog parsing and installation.

Exit codes are `0` success/valid, `1` invalid arguments, `2` file missing, `3` invalid key, `4` invalid signature, and `5` unexpected error.
