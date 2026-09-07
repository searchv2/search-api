# Claude Coding Standards

When working in this repository, write code like a senior developer:

1. **Prefer existing patterns over new ones**  
   Before writing a new solution, look for existing conventions in naming, structure, error handling, and logging. Follow established patterns instead of introducing a new one.
2. **Apply SOLID principles**  
   Keep classes and functions focused on a single responsibility. Use dependency inversion where appropriate. Avoid god classes and deeply nested conditionals.
3. **Simplicity first**  
   Choose the simplest solution that correctly solves the problem. Avoid speculative abstractions, unnecessary configurability, and premature optimization.
4. **Naming and readability**  
   Use descriptive names and small, readable functions. Keep comments minimal and focused on explaining *why*, not *what*.
5. **Tests included**  
   Any new logic should include unit tests that follow this repository’s existing test conventions.
6. **Error handling**  
   Handle errors explicitly and consistently with existing repository patterns. Do not swallow exceptions.
7. **No unnecessary dependencies**  
   Do not add packages or libraries unless there is a clear, justified need.
8. **Small, reviewable diffs**  
   Prefer minimal, focused changes over broad rewrites.
