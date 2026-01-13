# Build
FROM maven:3.9.6-eclipse-temurin-21 AS build
WORKDIR /usr/src/app

COPY pom.xml .
RUN mvn dependency:go-offline

COPY src src
RUN mvn package -DskipTests

# Runtime
FROM registry.access.redhat.com/ubi9/openjdk-21:1.23
ENV LANGUAGE='en_US:en'

COPY --from=build --chown=185 /usr/src/app/target/quarkus-app/lib/ /deployments/lib/
COPY --from=build --chown=185 /usr/src/app/target/quarkus-app/*.jar /deployments/
COPY --from=build --chown=185 /usr/src/app/target/quarkus-app/app/ /deployments/app/
COPY --from=build --chown=185 /usr/src/app/target/quarkus-app/quarkus/ /deployments/quarkus/

USER 185

ENV JAVA_OPTS_APPEND="-Djava.util.logging.manager=org.jboss.logmanager.LogManager -Dquarkus.init-and-exit=true"
ENV JAVA_APP_JAR="/deployments/quarkus-run.jar"

ENTRYPOINT [ "/opt/jboss/container/java/run/run-java.sh" ]
