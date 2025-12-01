# TingoAI Payment Gateway - SSH Deployment Setup

## Prerequisites on Server

1. **Install .NET 9.0 Runtime**
```bash
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
sudo ./dotnet-install.sh --channel 9.0 --runtime aspnetcore
```

2. **Create deployment directory**
```bash
sudo mkdir -p /var/www/paymentgateway
sudo chown -R $USER:$USER ~/paymentgateway
sudo chown -R www-data:www-data /var/www/paymentgateway
```

3. **Install systemd service**
```bash
sudo cp ~/paymentgateway/deployment/tingoai-payment-gateway.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable tingoai-payment-gateway
```

## GitHub Secrets Required

Add these secrets to your GitHub repository:
**Settings → Secrets and variables → Actions → New repository secret**

1. **SSH_HOST** - Your server IP or domain (e.g., `tingopayment.tingoradio.ai` or `12.34.56.78`)
2. **SSH_USER** - SSH username (e.g., `ubuntu`, `ec2-user`, or `root`)
3. **SSH_KEY** - Your private SSH key (entire content of your `.pem` file)

Example SSH_KEY format:
```
-----BEGIN RSA PRIVATE KEY-----
MIIEpAIBAAKCAQEA...
...
-----END RSA PRIVATE KEY-----
```

## Nginx Configuration (Optional)

If using Nginx as reverse proxy, create `/etc/nginx/sites-available/paymentgateway`:

```nginx
server {
    listen 80;
    server_name tingopayment.tingoradio.ai;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Enable the site:
```bash
sudo ln -s /etc/nginx/sites-available/paymentgateway /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

## SSL Certificate (Optional with Certbot)

```bash
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d tingopayment.tingoradio.ai
```

## Deployment Commands

**Manual deployment from local machine:**
```bash
git push origin master  # Triggers automatic deployment
```

**Or trigger manually from GitHub:**
Go to Actions → Deploy to AWS via SSH → Run workflow

## Useful Server Commands

```bash
# Check service status
sudo systemctl status tingoai-payment-gateway

# View logs
sudo journalctl -u tingoai-payment-gateway -f

# Restart service
sudo systemctl restart tingoai-payment-gateway

# Stop service
sudo systemctl stop tingoai-payment-gateway
```

## Troubleshooting

1. **Service won't start**: Check logs with `sudo journalctl -u tingoai-payment-gateway -n 50`
2. **Permission denied**: Ensure www-data owns `/var/www/paymentgateway`
3. **Port already in use**: Check if another service is using port 5000: `sudo lsof -i :5000`
