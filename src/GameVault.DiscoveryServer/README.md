# GameVault DiscoveryServer

`GameVault.DiscoveryServer` is a separate LAN discovery/status server for GameVault. It is not part of `GameVault.Desktop` and it does not change any `GameVault.Api` REST endpoints.

## Ports

- UDP `47891`: LAN discovery
- TCP `47892`: status/config JSON

## Protocol

Desktop sends UDP broadcast:

```txt
GAMEVAULT_DISCOVER_REQUEST
```

DiscoveryServer responds with short JSON:

```json
{
  "serverName": "GameVault HomeLab",
  "serverIp": "192.168.88.110",
  "tcpStatusPort": 47892
}
```

Desktop then connects to TCP `serverIp:tcpStatusPort` and sends:

```txt
GAMEVAULT_STATUS_V1
```

The server responds with one JSON line containing API/files/manifest URLs and basic catalog status.

## Configuration

Defaults are in `appsettings.json`. Environment variables override them:

```txt
GAMEVAULT_DISCOVERY__SERVERNAME=GameVault HomeLab
GAMEVAULT_DISCOVERY__SERVERIP=192.168.88.110
GAMEVAULT_DISCOVERY__APIBASEURL=http://192.168.88.110
GAMEVAULT_DISCOVERY__FILESBASEURL=http://192.168.88.110/files
GAMEVAULT_DISCOVERY__MANIFESTURL=http://192.168.88.110/files/manifests/downloads.json
GAMEVAULT_DISCOVERY__UDPDISCOVERYPORT=47891
GAMEVAULT_DISCOVERY__TCPSTATUSPORT=47892
GAMEVAULT_DISCOVERY__SERVERVERSION=1.0.0
```

If `ServerIp` is empty, the server tries to advertise a local IPv4 address.

## Run Locally

```powershell
dotnet run --project src/GameVault.DiscoveryServer
```

## Linux VM Notes

Publish and run on the VM:

```bash
dotnet publish src/GameVault.DiscoveryServer/GameVault.DiscoveryServer.csproj -c Release -o ./publish-discovery
cd publish-discovery
GAMEVAULT_DISCOVERY__SERVERIP=192.168.88.110 dotnet GameVault.DiscoveryServer.dll
```

Allow firewall traffic:

```bash
sudo ufw allow 47891/udp
sudo ufw allow 47892/tcp
```

## Docker Notes

If you add this to an existing compose file, expose/map:

```yaml
ports:
  - "47891:47891/udp"
  - "47892:47892/tcp"
```

Use the environment variables above to point Desktop to the same API/nginx/static server URLs already used by GameVault.
