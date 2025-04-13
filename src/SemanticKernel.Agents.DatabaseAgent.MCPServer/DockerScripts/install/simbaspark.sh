#!/bin/bash

# Parameters for version and OS
VERSION=${1:-2.9.1}
OS=${2:-Debian-64bit}
BUILD=${3:-1001}

# Extract the major version number from the version
MAJOR_VERSION=$(echo $VERSION | cut -d '.' -f 1)

# Construct the download URL
URL="https://databricks-bi-artifacts.s3.us-east-2.amazonaws.com/simbaspark-drivers/odbc/${VERSION}/SimbaSparkODBC-${VERSION}.${BUILD}-${OS}.zip"

echo "Downloading Simba Spark ODBC Driver version $VERSION for OS $OS"
echo "Download URL: $URL"

# Download the package
wget $URL

# Unzip the package
unzip SimbaSparkODBC-${VERSION}.${BUILD}-${OS}.zip

# Move the driver to the appropriate directory
mkdir -p /opt/sparkodbc
mv *.deb /opt/sparkodbc

# Remove the downloaded zip file and extracted directory
rm -rf SimbaSparkODBC-${VERSION}.${BUILD}-${OS}.zip
rm -rf docs

# Install dependencies
apt-get update && \
	apt-get install -y --no-install-recommends libsasl2-2 libsasl2-modules-gssapi-mit libodbc2 \
	&& rm -rf /var/lib/apt/lists/*

# Install the package from deb file
dpkg -i /opt/sparkodbc/*.deb
