# This Dockerfile is just used to facilitate the GitHub Actions CI build
# (it produces the binary outside of the Docker build)

FROM alpine:latest

# Dependencies and curl for custom backup scripts
RUN apk add --no-cache libintl libstdc++ curl

# Set and add working directory
WORKDIR /root/
RUN mkdir data

# Copy files
COPY Floofbot/Scripts/LinuxDBBackup.sh .
COPY artifacts/linux-musl-x64/Floofbot .
COPY artifacts/linux-musl-x64/libe_sqlite3.so .

# Fix for running in a container
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# Add version from CI build
ARG floofbot_version=unknown
ENV FLOOFBOT_VERSION=$floofbot_version

ENTRYPOINT ["./Floofbot"]
CMD ["/root/data/config.yaml"]
