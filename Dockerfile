# This Dockerfile is just used to facilitate the GitHub Actions CI build
# (it produces the binary outside of the Docker build)

FROM alpine:latest

# dependencies
RUN apk add --no-cache libintl libstdc++

# Set and add working directory
WORKDIR /root/
RUN mkdir data

# Copy files
COPY Floofbot/Scripts/LinuxDBBackup.sh .
COPY artifacts/linux-musl-x64/Floofbot .

# Fix for running in a container
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

ENTRYPOINT ["./Floofbot"]
CMD ["data/config.yaml"]