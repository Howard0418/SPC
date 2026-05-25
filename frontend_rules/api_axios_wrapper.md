# Frontend API wrapper

## Rules for AI Agents
1. **Centralized Axios**: All API calls MUST go through the `api/client.js` wrapper.
2. **Interceptors**: Handle 401 Unauthorized globally to redirect to login. Handle token expiration automatically if refresh tokens are used.
