# AI Module for SpaceWar Discord Bot

This module provides AI-powered natural language processing for game state modifications using OpenRouter API with GPT-4o mini.

## Overview

The AI module allows players to use natural language commands to modify the game state through the existing fixup commands. Instead of remembering specific command syntax, players can describe what they want to do in plain English.

## Usage

Use the `/ai` command followed by a natural language description:

```
/ai move all forces from hex 0,1 to hex 0,2
/ai give player 2 the freeze dried forces tech
/ai set my science to 10
/ai make it player 3's turn
/ai exhaust the planet at 1,-1
```

## Architecture

### Components

- **`Models/`** - Request/response models for OpenRouter API
- **`Services/OpenRouterService.cs`** - Handles API communication and function parsing
- **`Tools/FixupToolDefinitions.json`** - JSON tool definitions for AI function calling
- **`Commands/AiCommands.cs`** - Discord command handler

### Flow

1. Player uses `/ai` command with natural language request
2. Game context is gathered and sent to OpenRouter API
3. AI interprets request and suggests fixup command calls
4. Commands are executed using existing `FixupCommands` methods
5. Results are reported back to the player

## Configuration

Add your OpenRouter API key to `Secrets.json`:

```json
{
  "OpenRouterApiKey": "your-api-key-here"
}
```

## Supported Operations

The AI can interpret requests for all fixup command operations:
- Setting forces on planets
- Granting/removing technologies
- Exhausting/unexhausting planets and techs
- Changing player turns
- Modifying science and victory points

## Safety

- AI suggestions are validated before execution
- Only game state modifications through existing fixup commands
- Conservative approach - unclear requests result in no actions
- All changes are logged and can be tracked 