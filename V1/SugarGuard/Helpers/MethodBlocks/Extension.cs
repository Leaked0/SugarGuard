using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using static SugarGuard.Helpers.MethodBlocks.BlockParser;

namespace SugarGuard.Helpers.MethodBlocks
{
    public static class Extension
    {
        public static List<Block> GetBlocks(this MethodDef method)
        {
            List<Block> blocks = new List<Block>();
            var body = method.Body;
            body.SimplifyBranches();
            ScopeBlock root = ParseBody(body);
            var branchesInstructions = new List<Instruction>();
            var exceptions = method.Body.ExceptionHandlers;
            var exceptionsCount = 0;
            Trace trace = new Trace(body, method.ReturnType.RemoveModifiers().ElementType != ElementType.Void);
            foreach (InstrBlock block in GetAllBlocks(root))
            {
                var statements = SplitStatements(block, trace);
                foreach (var statement in statements)
                {
                    var statementLast = new HashSet<Instruction>(statements.Select(st => st.Last()));
                    int finished = 0;
                    int finishedExceptions = 0;
                    foreach (Instruction instr in statement)
                    {
                        if (instr.Operand is Instruction)
                        {
                            branchesInstructions.Add(instr.Operand as Instruction);
                        }
                        if (branchesInstructions.Contains(instr))
                        {
                            branchesInstructions.Remove(instr);
                            finished++;
                        }
                        if (exceptions.Count > 1)
                            continue;
                        if (exceptions.Any(x => x.TryStart == instr))
                            exceptionsCount++;
                        if (exceptions.Any(x => x.HandlerEnd == instr))
                        {
                            finishedExceptions++;
                            exceptionsCount--;
                        }
                    }
                    bool isInException = exceptionsCount > 0 || finishedExceptions > 0;
                    bool isInBranch = branchesInstructions.Count > 0 || finished > 0;
                    bool hasUnknownSource(IList<Instruction> instrs) => instrs.Any(instr =>
                      {
                          if (trace.HasMultipleSources(instr.Offset))
                              return true;
                          if (trace.BrRefs.TryGetValue(instr.Offset, out List<Instruction> srcs))
                          {
                              if (srcs.Any(src => src.Operand is Instruction[]))
                                  return true;
                              if (srcs.Any(src => src.Offset <= statements.First.Value.Last().Offset ||
                                                  src.Offset >= block.Instructions.Last().Offset))
                                  return true;
                              if (srcs.Any(src => statementLast.Contains(src)))
                                  return true;
                          }
                          return false;
                      });

                    blocks.Add(new Block(statement, isInException, !hasUnknownSource(statement), isInBranch));
                }
            }

            return blocks;
        }

        static LinkedList<Instruction[]> SplitStatements(InstrBlock block, Trace trace)
        {
            var statements = new LinkedList<Instruction[]>();
            var currentStatement = new List<Instruction>();

            var requiredInstr = new HashSet<Instruction>();

            for (var i = 0; i < block.Instructions.Count; i++)
            {
                var instr = block.Instructions[i];
                currentStatement.Add(instr);

                var shouldSplit = i + 1 < block.Instructions.Count && trace.HasMultipleSources(block.Instructions[i + 1].Offset);
                switch (instr.OpCode.FlowControl)
                {
                    case FlowControl.Branch:
                    case FlowControl.Cond_Branch:
                    case FlowControl.Return:
                    case FlowControl.Throw:
                        shouldSplit = true;
                        if (trace.AfterStack[instr.Offset] != 0)
                        {
                            if (instr.Operand is Instruction targetInstr)
                                requiredInstr.Add(targetInstr);
                            else if (instr.Operand is Instruction[] targetInstrs)
                            {
                                foreach (var target in targetInstrs)
                                    requiredInstr.Add(target);
                            }
                        }
                        break;
                }

                requiredInstr.Remove(instr);

                if (instr.OpCode.OpCodeType != OpCodeType.Prefix && trace.AfterStack[instr.Offset] == 0 && requiredInstr.Count == 0 && (shouldSplit || 90 > new Random().NextDouble()) && (i == 0 || block.Instructions[i - 1].OpCode.Code != Code.Tailcall))
                {
                    statements.AddLast(currentStatement.ToArray());
                    currentStatement.Clear();
                }
            }

            if (currentStatement.Count > 0)
                statements.AddLast(currentStatement.ToArray());

            return statements;
        }

        static IEnumerable<InstrBlock> GetAllBlocks(ScopeBlock scope)
        {
            foreach (BlockBase child in scope.Children)
            {
                if (child is InstrBlock)
                    yield return (InstrBlock)child;
                else
                {
                    foreach (InstrBlock block in GetAllBlocks((ScopeBlock)child))
                        yield return block;
                }
            }
        }
    }
}
