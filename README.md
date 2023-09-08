# just wanted a more serious interface without "hacker" icons

# Invisible Man - XRay Client

> A client for xray core

Invisible Man XRay is an open-source and free client that supports [xray core](https://github.com/XTLS/Xray-core). It provides an easy-to-use interface to configure and manage proxies and allows users to switch between different server locations.

![Image1](https://github.com/InvisibleManVPN/InvisibleMan-XRayClient/blob/master/Images/image-1.png)
![Image2](https://github.com/InvisibleManVPN/InvisibleMan-XRayClient/blob/master/Images/image-2.png)

## Getting started

- If you are new to this, please download the application from [releases](https://github.com/InvisibleManVPN/InvisibleMan-XRayClient/releases/latest).

- But if you want to get the source of the client, follow these steps:
  - Download the [requirements](#requirements)
  - Clone a copy of the repository:
    ```
    git clone "https://github.com/InvisibleManVPN/InvisibleMan-XRayClient.git"
    ```
  - Change to the directory:
    ```
    cd InvisibleMan-XRayClient
    ```
  - Make `XRayCore.dll` file and copy to the `/InvisibleMan-XRay/Libraries` directory:
    ```
    cd XRay-Wrapper
    go build --buildmode=c-shared -o XRayCore.dll -trimpath -ldflags "-s -w -buildid=" .
    md ..\InvisileMan-XRay\Libraries
    copy XRayCore.dll ..\InvisibleMan-XRay\Libraries   
    ```
    
  - Download `InvisibleMan-TUN` service (based on your OS) from [this](https://github.com/InvisibleManVPN/InvisibleMan-TUN/releases/latest) link, extract and copy to the `/InvisibleMan-XRay/TUN` directory.

  - Download `geoip.dat` and `geosite.dat` files and copy to the `/InvisibleMan-XRay` directory:
    ```
    cd ..\InvisibleMan-XRay
    curl https://github.com/v2fly/geoip/releases/latest/download/geoip.dat -o geoip.dat -L
    curl https://github.com/v2fly/domain-list-community/releases/latest/download/dlc.dat -o geosite.dat -L
    ```
  - Run the project:
    ```
    dotnet run
    ```

## Requirements

- Go https://go.dev/dl/
- .Net https://dotnet.microsoft.com/download
- Curl https://curl.se/download.html
