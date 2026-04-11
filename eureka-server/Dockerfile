# Sử dụng jar đã build sẵn
FROM eclipse-temurin:17-jre
WORKDIR /app
COPY eureka-server/target/*.jar app.jar
EXPOSE 8761
ENTRYPOINT ["java","-jar","app.jar"]