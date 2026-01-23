# Build
FROM maven:3.9.6-eclipse-temurin-21 AS build
WORKDIR /usr/src/app

COPY pom.xml .
RUN mvn dependency:go-offline

COPY src src
RUN mvn package -DskipTests

# Runtime
FROM eclipse-temurin:21-jre-alpine
WORKDIR /app

# Create a non-root user
RUN addgroup --system --gid 1001 appgroup && \
    adduser --system --uid 1001 --ingroup appgroup appuser

# Copy built artifacts
COPY --from=build --chown=appuser:appgroup /usr/src/app/target/quarkus-app/lib/ /app/lib/
COPY --from=build --chown=appuser:appgroup /usr/src/app/target/quarkus-app/*.jar /app/
COPY --from=build --chown=appuser:appgroup /usr/src/app/target/quarkus-app/app/ /app/app/
COPY --from=build --chown=appuser:appgroup /usr/src/app/target/quarkus-app/quarkus/ /app/quarkus/

USER appuser

ENV JAVA_OPTS="-Djava.util.logging.manager=org.jboss.logmanager.LogManager"

ENTRYPOINT ["java", "-jar", "/app/quarkus-run.jar"]
