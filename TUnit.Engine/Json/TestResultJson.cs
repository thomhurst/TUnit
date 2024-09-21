﻿using TUnit.Core.Enums;

namespace TUnit.Engine.Json;

public record TestResultJson
{
    public required Status Status { get; init; }
    public required DateTimeOffset Start { get; init; }
    public required DateTimeOffset End { get; init; }
    public required TimeSpan Duration { get; init; }
    public required ExceptionJson? Exception { get; init; }
    public required string ComputerName { get; init; }
    public required string? Output { get; init; }
}