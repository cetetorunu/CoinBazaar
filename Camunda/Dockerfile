FROM registry.hub.docker.com/camunda/camunda-bpm-platform:tomcat-7.12.0
WORKDIR /camunda
# Copy all contents to content folder

COPY .  .

RUN ls

COPY ["/bpm-platform.xml", "conf/"]



#FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build

#COPY ./cert/d-teknolojicomtr.crt  /usr/local/share/ca-certificates/d-teknolojicomtr.crt

#RUN chmod 644 /usr/local/share/ca-certificates/d-teknolojicomtr.crt
#RUN update-ca-certificates

#RUN  keytool -import  -file "/camunda/cert/tls.crt" -keystore "/etc/ssl/certs/java/cacerts" -storepass "changeit" -noprompt
#CMD ["/camunda/cert/keytoolimport.sh"]
