await AdapterChecks.Run();
await PipelineChecks.Run();
await CheckoutChecks.Run();
Console.WriteLine($"{Check.Count} supplier normalization checks passed. No databases or live suppliers were used.");
