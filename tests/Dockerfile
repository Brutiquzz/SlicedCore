# Template smoke-test sandbox
# Build context must be the repository root:
#   docker build -f tests/Dockerfile -t template-tests .
#   docker run --rm template-tests
#
# The entire repo is copied into the image so dotnet new install works
# against the on-disk template manifests without touching the host environment.

FROM mcr.microsoft.com/dotnet/sdk:10.0

# Install PowerShell (pwsh) — required by Test-Templates.ps1
RUN apt-get update \
	&& apt-get install -y --no-install-recommends \
		wget \
		ca-certificates \
	&& wget -q "https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb" \
	&& dpkg -i packages-microsoft-prod.deb \
	&& rm packages-microsoft-prod.deb \
	&& apt-get update \
	&& apt-get install -y --no-install-recommends powershell \
	&& apt-get clean \
	&& rm -rf /var/lib/apt/lists/*

WORKDIR /repo

# Copy the whole repository into the image
COPY . .

# Run the template smoke tests; all dotnet new installs stay inside
# this container — the host registry is never touched.
CMD ["pwsh", "-NoProfile", "-NonInteractive", "-File", "/repo/tests/Test-Templates.ps1"]
