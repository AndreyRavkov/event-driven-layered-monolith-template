---
paths:
  - "solution/AR/API/AR.ApiServices/**/*.cs"
---

## Rule for **API Service Layer**

- Each Service should be internal with a public interface. Interface and class live in the same file.
- The layer is a thin adapter — no business logic. Just a simple mapping between the request and the response. Or validation request. Or multiple requests to `AR.AppServices` with combining results to response.
- Each API Service should has `ApiService` postfix. DI isn’t needed, because it resolved automatically inside `ApiServicesDIModule.cs` file.
- All request Models should be located in the `Models` folder under each business subfolder. Each model should be inherited from `BaseValidationModel`. Validators should be located in the `Validators` folder under each business subfolder.