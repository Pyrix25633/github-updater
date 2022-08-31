FROM mcr.microsoft.com/dotnet/sdk:6.0

RUN apt-get -y update
RUN apt-get -y install zip
WORKDIR /app
COPY *.csproj ./
COPY *.cs ./
COPY launch-compilation.sh ./
RUN chmod +x /app/launch-compilation.sh
RUN dotnet add package Newtonsoft.Json
RUN dotnet restore

CMD [ "/app/launch-compilation.sh" ]