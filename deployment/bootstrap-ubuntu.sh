#!/usr/bin/env bash
set -euo pipefail

# Bootstrap script for Ubuntu server to enable C#/.NET deployment
# Usage: sudo bash bootstrap-ubuntu.sh

GIT_REPO_PLACEHOLDER="git@github.com:your-org/your-repo.git"
BRANCH=master
APP_DIR="$HOME/tingoaipaymentgateway"

echo "Updating apt and installing prerequisites..."
apt-get update -y
apt-get install -y \
  apt-transport-https \
  ca-certificates \
  gnupg \
  wget \
  curl \
  software-properties-common \
  lsb-release \
  git \
  unzip \
  docker.io \
  docker-compose

echo "Enabling and starting Docker..."
systemctl enable --now docker

# Install Microsoft package repo and .NET SDK/runtime (8.0 recommended)
echo "Installing Microsoft package repository and dotnet SDK/runtime..."
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
dpkg -i /tmp/packages-microsoft-prod.deb
apt-get update -y
# Install dotnet runtime + SDK (SDK optional for build on server)
apt-get install -y dotnet-sdk-8.0 dotnet-runtime-8.0 || apt-get install -y dotnet-sdk-7.0 dotnet-runtime-7.0 || true

echo "Creating application directory: $APP_DIR"
mkdir -p "$APP_DIR"
chown -R $(logname 2>/dev/null || echo ubuntu):$(id -gn $(logname 2>/dev/null || echo ubuntu)) "$APP_DIR" || true

cat <<'EOF'
Next steps (replace placeholders):

- To clone the repository into the folder (SSH-based):
  git clone -b "${BRANCH}" "${GIT_REPO_PLACEHOLDER}" "$APP_DIR"

- Or using HTTPS (if you prefer):
  git clone -b "${BRANCH}" https://github.com/your-org/your-repo.git "$APP_DIR"

- After cloning, change to the API folder and either publish or use Docker:
  cd "$APP_DIR"

# Option A — dotnet publish and run (example):
  cd src/TingoAI.PaymentGateway/
  dotnet publish -c Release -o ./publish
  # run with: dotnet ./publish/TingoAI.PaymentGateway.dll

# Option B — use Docker (if repo contains Dockerfile / docker-compose.yml):
  cd "$APP_DIR"
  docker-compose pull || true
  docker-compose up -d --build

- Place your environment file as recommended by the project:
  # copy the .env contents to /etc/tingo/tingoaipayment.env
  sudo mkdir -p /etc/tingo
  sudo tee /etc/tingo/tingoaipayment.env > /dev/null <<ENV
  # Paste env contents here
ENV

EOF

echo "Bootstrap finished. Review the 'Next steps' messages to clone and run the app."
