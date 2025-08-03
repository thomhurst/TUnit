# Check Requirements Status

Show current requirement gathering progress and continue.

## Instructions:

1. Read requirements/.current-requirement
2. If no active requirement:
   - Show message: "No active requirement gathering"
   - Suggest /requirements-start or /requirements-list
   - Exit

3. If active requirement exists:
   - Read metadata.json for current phase and progress
   - Show formatted status
   - Load appropriate question/answer files
   - Continue from last unanswered question

## Status Display Format:
```
📋 Active Requirement: [name]
Started: [time ago]
Phase: [Discovery/Detail]
Progress: [X/Y] questions answered

[Show last 3 answered questions with responses]

Next Question:
[Show next unanswered question with default]
```

## Continuation Flow:
1. Read next unanswered question from file
2. Present to user with default
3. Accept yes/no/idk response
4. Update answer file
5. Update metadata progress
6. Move to next question or phase

## Phase Transitions:
- Discovery complete → Run context gathering → Generate detail questions
- Detail complete → Generate final requirements spec