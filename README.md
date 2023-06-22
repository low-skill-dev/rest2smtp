[![Docker](https://img.shields.io/badge/docker-%230db7ed.svg?style=for-the-badge&logo=docker&logoColor=white)](https://hub.docker.com/repository/docker/luminodiode/rest2wireguard)
[![Alpine Linux](https://img.shields.io/badge/Alpine_Linux-%230D597F.svg?style=for-the-badge&logo=alpine-linux&logoColor=white)](https://www.alpinelinux.org)
[![Nginx](https://img.shields.io/badge/nginx-%23009639.svg?style=for-the-badge&logo=nginx&logoColor=white)](https://nginx.org)
[![.Net](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)](https://dotnet.microsoft.com/en-us/apps/aspnet)

## rest2smtp
### Uncomplicated Alpine-based TLS-securely WebAPI-managed SMTP-relay mail sending server.

## Full list of endpoints:
  - **GET /api/mail/limitations** - get current sending limits.
  - **POST /api/mail** - send the email using the next model in body:
    
        public string From { get; set; } = null!;
        public string FromName { get; set; } = null!;
        public string To { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string Body { get; set; } = null!;
      
## Full list of environment variables
- ### ASP WebAPI
    - **REST2WG_IGNORE_UNAUTHORIZED** - server will not send any data back on an unauthorized request. It will be invisible.
        - Valid range: true/false.
        - Default: false.
        - 
## Full list of listened ports
- **5001** - tcp nginx-to-api HTTP2 self-signed TLS port.
- **5002** - tcp nginx-to-api no-TLS port.

## Required secrets file example
    {
      "MasterAccounts": [
        {
          "KeyHashBase64": "YOUR_KEYHASH_HERE_SEE_https://dotnetfiddle.net/ldbnVB"
        }
      ],
      "SmtpServiceSettings": {
        "SmtpRelays": [
          {
            "Login": "your_login",
            "Password": "your_password",
            "SmtpHost": "your_smtp_relay_host",
            "SmtpPort": 587,
            "MaxMailsPerDay": "the_limitation"
    } ] } }

## Authorization
Provide base64-ecndoded key, hashing which with SHA512 will evaluate into *KeyHashBase64* from the secrets file. Use *Authorization* header with *Basic* auth type.
