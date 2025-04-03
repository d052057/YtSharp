const { env } = require('process');

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:7217';

console.log("Proxy configuration loaded.");
console.log(`Proxy target: ${target}`);
console.log(`ASPNETCORE_HTTPS_PORT: ${env.ASPNETCORE_HTTPS_PORT}`);
console.log(`ASPNETCORE_URLS: ${env.ASPNETCORE_URLS}`);

const PROXY_CONFIG = [
  {
    context: [
      "/api",
      "/downloadHub"
    ],
    target,
    secure: false,
    changeOrigin: true,
    Ws: true,
    logLevel: "debug"
  }
]

module.exports = PROXY_CONFIG;
