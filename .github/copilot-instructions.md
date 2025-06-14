## The Three Laws of Robotics
These laws are designed to ensure that the AI behaves in a way that is safe and beneficial for humans. They should be followed at ALL times. Always
consider these laws when making decisions or providing assistance.
1. Changes should be incremental and reversible. If a change is made that causes an issue, it should be easy to revert.
2. Always run tests to ensure that the code is functioning as expected. If the tests fail, do not proceed with further changes until the issue is resolved.
3. After making changes and passing tests, always commit the changes with a clear and concise message that explains what was changed and why.
  - The commit message should be in the format of a high level summary followed by a detailed description of the changes made, including file names and a brief description of each change.

## Code Style
- Use idiomatic C# code.
- Use clear and concise variable names.
- Use standard conventions for the language you are working in.
- Code using SOLID principles
- Code should follow accessibility best practices.
- Validate all inputs
- Validate that operations have succeeded where appropriate
- If validations fail, throw an exception unless there is a simple and clear way to continue
- Use comments to explain complex logic or important decisions.
- Every function should have a docstring that describes its purpose, parameters, and return values consistent with the language's conventions.
- Code readability is a priority; avoid overly complex solutions when simpler ones suffice.

## Agent Behavior
- Always consider if a task would be better done in Visual Studio. Any XML file that is typically modified through the Visual Studio UI should be edited there. If so instruct the user to open Visual Studio and perform the task there. Wait for them to complete the task before continuing.
  After they have completed the task always check that the task has been completed successfully.
- Whenever possible code in a TDD (Test Driven Development) style, meaning:
  - Write tests first
  - Write the minimum amount of code to pass the tests
  - Refactor the code to improve readability and maintainability
- When following TDD, write the tests and do not write the implementation code until the user has gotten a chance to
  give feedback on them and confirmed that they want to proceed with the implementation. When writing tests, ensure that they are comprehensive and cover all edge cases.
- I am very new to C# and dotnet development, so please explain things as you go along to help me learn. My primary goal is to learn how to develop in dotnet.
  I am familiar with Python, so you can use that as a reference point for explaining concepts. After we implement a feature, please summarize what we did and why, so I can understand
  the reasoning behind the decisions made.
- Always ask for clarification if the request is ambiguous or incomplete.
- Make a small number of changes at a time to avoid overwhelming the user.