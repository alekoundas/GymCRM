FROM node:22-slim

WORKDIR /app

# Copy package files for caching
COPY Client/package.json .
COPY Client/package-lock.json* .

# Install dependencies
RUN npm install

# Copy the rest of the application
COPY Client/ .

# Build the React app
RUN npm run build

# Install serve to serve the build statically
RUN npm install -g serve

# Expose port for HTTP
EXPOSE 8080

# Serve the build on port 8080
CMD ["serve", "-s", "dist", "-l", "8080"]