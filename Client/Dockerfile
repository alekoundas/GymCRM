FROM node:18-alpine

WORKDIR /app

# Copy package files for caching.
COPY package.json ./
COPY package-lock.json* ./

# Install dependencies.
RUN npm install

# Copy the rest of the application.
COPY . .

# Build the React app.
RUN npm run build


# Expose ports for HTTP and HTTPS.
EXPOSE 8080
#EXPOSE 8081


# No install and CMD needed, Nginx will serve the build.
# Install serve to serve the build statically
#RUN npm install -g serve

# Serve the build on port 8080 (Nginx will handle HTTPS redirection)
#CMD ["serve", "-s", "build", "-l", "8080"]