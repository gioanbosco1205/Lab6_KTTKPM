# Stage 1: Build bằng Maven
FROM maven:3.9.6-eclipse-temurin-17 AS build
WORKDIR /app
COPY eureka-server/pom.xml .
COPY eureka-server/src ./src
RUN mvn clean package -DskipTests

# Stage 2: Chạy app
FROM eclipse-temurin:17-jdk
WORKDIR /app
COPY --from=build /app/target/*.jar app.jar
EXPOSE 8761
ENTRYPOINT ["java","-jar","app.jar"]