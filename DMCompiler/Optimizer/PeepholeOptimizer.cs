using System.Collections.Generic;
using System.Linq;
using DMCompiler.Bytecode;
using OpenDreamShared.Compiler;

namespace DMCompiler.DM.Optimizer {
    internal interface PeepholeOptimization {
        public int GetLength();
        public List<DreamProcOpcode> GetOpcodes();
        public void Apply(List<IAnnotatedBytecode> input, int index);

        public bool CheckPreconditions(List<IAnnotatedBytecode> input, int index) {
            return true;
        }
    }

    // Assign [ref]
    // Pop
    // -> AssignPop [ref]
    internal class AssignPop : PeepholeOptimization {
        public int GetLength() {
            return 2;
        }

        public List<DreamProcOpcode> GetOpcodes() {
            return new() {
                DreamProcOpcode.Assign,
                DreamProcOpcode.Pop
            };
        }

        public void Apply(List<IAnnotatedBytecode> input, int index) {
            var range = input.GetRange(index, 2).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            AnnotatedBytecodeReference? assignTarget = (range[0].GetArgs()[0] as AnnotatedBytecodeReference);
            input.RemoveRange(index, 2);
            input.Insert(index,
                new AnnotatedBytecodeInstruction(DreamProcOpcode.AssignPop,
                    new List<IAnnotatedBytecode> { assignTarget }));
        }
    }

    // PushNull
    // AssignPop [ref]
    // -> AssignNull [ref]
    internal class AssignNull : PeepholeOptimization {
        public int GetLength() {
            return 2;
        }

        public List<DreamProcOpcode> GetOpcodes() {
            return new() {
                DreamProcOpcode.PushNull,
                DreamProcOpcode.AssignPop
            };
        }

        public void Apply(List<IAnnotatedBytecode> input, int index) {
            var range = input.GetRange(index, 2).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            AnnotatedBytecodeReference? assignTarget = (range[1].GetArgs()[0] as AnnotatedBytecodeReference);
            input.RemoveRange(index, 2);
            input.Insert(index,
                new AnnotatedBytecodeInstruction(DreamProcOpcode.NullRef,
                    new List<IAnnotatedBytecode> { assignTarget }));
        }
    }

    // PushReferenceValue [ref]
    // DereferenceField [field]
    // -> PushRefAndDereferenceField [ref, field]
    internal class PushField : PeepholeOptimization {
        public int GetLength() {
            return 2;
        }

        public List<DreamProcOpcode> GetOpcodes() {
            return new() {
                DreamProcOpcode.PushReferenceValue,
                DreamProcOpcode.DereferenceField
            };
        }

        public void Apply(List<IAnnotatedBytecode> input, int index) {
            var range = input.GetRange(index, 2).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            AnnotatedBytecodeReference? pushVal = (range[0].GetArgs()[0] as AnnotatedBytecodeReference);
            AnnotatedBytecodeString? derefField = (range[1].GetArgs()[0] as AnnotatedBytecodeString);
            input.RemoveRange(index, 2);
            input.Insert(index,
                new AnnotatedBytecodeInstruction(DreamProcOpcode.PushRefAndDereferenceField,
                    new List<IAnnotatedBytecode> { pushVal, derefField }));
        }
    }

    // BooleanNot
    // JumpIfFalse [label]
    // -> JumpIfTrue [label]
    internal class BooleanNotJump : PeepholeOptimization {
        public int GetLength() {
            return 2;
        }

        public List<DreamProcOpcode> GetOpcodes() {
            return new() {
                DreamProcOpcode.BooleanNot,
                DreamProcOpcode.JumpIfFalse
            };
        }

        public void Apply(List<IAnnotatedBytecode> input, int index) {
            var range = input.GetRange(index, 2).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            AnnotatedBytecodeLabel? jumpLabel = (range[1].GetArgs()[0] as AnnotatedBytecodeLabel);
            input.RemoveRange(index, 2);
            input.Insert(index,
                new AnnotatedBytecodeInstruction(DreamProcOpcode.JumpIfTrue,
                    new List<IAnnotatedBytecode> { jumpLabel }));
        }
    }

    // PushReferenceValue [ref]
    // JumpIfFalse [label]
    // -> JumpIfReferenceFalse [ref] [label]
    internal class JumpIfReferenceFalse : PeepholeOptimization {
        public int GetLength() {
            return 2;
        }

        public List<DreamProcOpcode> GetOpcodes() {
            return new() {
                DreamProcOpcode.PushReferenceValue,
                DreamProcOpcode.JumpIfFalse
            };
        }

        public void Apply(List<IAnnotatedBytecode> input, int index) {
            var range = input.GetRange(index, 2).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            AnnotatedBytecodeReference? pushVal = (range[0].GetArgs()[0] as AnnotatedBytecodeReference);
            AnnotatedBytecodeLabel? jumpLabel = (range[1].GetArgs()[0] as AnnotatedBytecodeLabel);
            input.RemoveRange(index, 2);
            input.Insert(index,
                new AnnotatedBytecodeInstruction(DreamProcOpcode.JumpIfReferenceFalse,
                    new List<IAnnotatedBytecode> { pushVal, jumpLabel }));
        }
    }

    // PushString [string]
    // ...
    // PushString [string]
    // -> PushNStrings [count] [string] ... [string]
    internal class PushNStrings : PeepholeOptimization {
        public int GetLength() {
            return 2;
        }

        public List<DreamProcOpcode> GetOpcodes() {
            return new() {
                DreamProcOpcode.PushString,
                DreamProcOpcode.PushString
            };
        }

        public void Apply(List<IAnnotatedBytecode> input, int index) {
            int count = 0;
            int stackDelta = 0;
            while (index + count < input.Count && input[index + count] is AnnotatedBytecodeInstruction instruction &&
                   instruction.Opcode == DreamProcOpcode.PushString) {
                count++;
            }

            var range = input.GetRange(index, count).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            List<IAnnotatedBytecode> args = new();
            args.Add(new AnnotatedBytecodeInteger(count, new Location()));
            for (int i = 0; i < count; i++) {
                args.Add(range[i].GetArgs()[0]);
                stackDelta++;
            }

            input.RemoveRange(index, count);
            input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.PushNStrings, stackDelta, args));
        }
    }

    // PushFloat [float]
    // ...
    // PushFloat [float]
    // -> PushNFloats [count] [float] ... [float]
    internal class PushNFloats : PeepholeOptimization {
        public int GetLength() {
            return 2;
        }

        public List<DreamProcOpcode> GetOpcodes() {
            return new() {
                DreamProcOpcode.PushFloat,
                DreamProcOpcode.PushFloat
            };
        }

        public void Apply(List<IAnnotatedBytecode> input, int index) {
            int count = 0;
            int stackDelta = 0;
            while (index + count < input.Count && input[index + count] is AnnotatedBytecodeInstruction instruction &&
                   instruction.Opcode == DreamProcOpcode.PushFloat) {
                count++;
            }

            var range = input.GetRange(index, count).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            List<IAnnotatedBytecode> args = new();
            args.Add(new AnnotatedBytecodeInteger(count, new Location()));
            for (int i = 0; i < count; i++) {
                args.Add(range[i].GetArgs()[0]);
                stackDelta++;
            }

            input.RemoveRange(index, count);
            input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.PushNFloats, stackDelta, args));
        }
    }

    // PushReferenceValue [ref]
    // ...
    // PushReferenceValue [ref]
    // -> PushNRef [count] [ref] ... [ref]
    internal class PushNRef : PeepholeOptimization {
        public int GetLength() {
            return 2;
        }

        public List<DreamProcOpcode> GetOpcodes() {
            return new() {
                DreamProcOpcode.PushReferenceValue,
                DreamProcOpcode.PushReferenceValue
            };
        }

        public void Apply(List<IAnnotatedBytecode> input, int index) {
            int count = 0;
            int stackDelta = 0;
            while (index + count < input.Count && input[index + count] is AnnotatedBytecodeInstruction instruction &&
                   instruction.Opcode == DreamProcOpcode.PushReferenceValue) {
                count++;
            }

            var range = input.GetRange(index, count).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            List<IAnnotatedBytecode> args = new();
            args.Add(new AnnotatedBytecodeInteger(count, new Location()));
            for (int i = 0; i < count; i++) {
                args.Add(range[i].GetArgs()[0]);
                stackDelta++;
            }

            input.RemoveRange(index, count);
            input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.PushNRefs, stackDelta, args));
        }
    }


    // PushString [string]
    // PushFloat [float]
    // -> PushStringFloat [string] [float]
    internal class PushStringFloat : PeepholeOptimization {
        public int GetLength() {
            return 2;
        }

        public List<DreamProcOpcode> GetOpcodes() {
            return new() {
                DreamProcOpcode.PushString,
                DreamProcOpcode.PushFloat
            };
        }

        public void Apply(List<IAnnotatedBytecode> input, int index) {
            var range = input.GetRange(index, 2).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            AnnotatedBytecodeString? pushVal1 = (range[0].GetArgs()[0] as AnnotatedBytecodeString);
            AnnotatedBytecodeFloat? pushVal2 = (range[1].GetArgs()[0] as AnnotatedBytecodeFloat);
            input.RemoveRange(index, 2);
            input.Insert(index,
                new AnnotatedBytecodeInstruction(DreamProcOpcode.PushStringFloat,
                    new List<IAnnotatedBytecode> { pushVal1, pushVal2 }));
        }
    }

    // PushFloat [float]
    // SwitchCase [label]
    // -> SwitchOnFloat [float] [label]
    internal class SwitchOnFloat : PeepholeOptimization {
        public int GetLength() {
            return 2;
        }

        public List<DreamProcOpcode> GetOpcodes() {
            return new() {
                DreamProcOpcode.PushFloat,
                DreamProcOpcode.SwitchCase
            };
        }

        public void Apply(List<IAnnotatedBytecode> input, int index) {
            var range = input.GetRange(index, 2).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            AnnotatedBytecodeFloat? pushVal = (range[0].GetArgs()[0] as AnnotatedBytecodeFloat);
            AnnotatedBytecodeLabel? jumpLabel = (range[1].GetArgs()[0] as AnnotatedBytecodeLabel);
            input.RemoveRange(index, 2);
            input.Insert(index,
                new AnnotatedBytecodeInstruction(DreamProcOpcode.SwitchOnFloat,
                    new List<IAnnotatedBytecode> { pushVal, jumpLabel }));
        }
    }

    // PushString [string]
    // SwitchCase [label]
    // -> SwitchOnString [string] [label]
    internal class SwitchOnString : PeepholeOptimization {
        public int GetLength() {
            return 2;
        }

        public List<DreamProcOpcode> GetOpcodes() {
            return new() {
                DreamProcOpcode.PushString,
                DreamProcOpcode.SwitchCase
            };
        }

        public void Apply(List<IAnnotatedBytecode> input, int index) {
            var range = input.GetRange(index, 2).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            AnnotatedBytecodeString? pushVal = (range[0].GetArgs()[0] as AnnotatedBytecodeString);
            AnnotatedBytecodeLabel? jumpLabel = (range[1].GetArgs()[0] as AnnotatedBytecodeLabel);
            input.RemoveRange(index, 2);
            input.Insert(index,
                new AnnotatedBytecodeInstruction(DreamProcOpcode.SwitchOnString,
                    new List<IAnnotatedBytecode> { pushVal, jumpLabel }));
        }
    }

    // PushStringFloat [string] [float]
    // ...
    // PushStringFloat [string] [float]
    // -> PushArbitraryNOfStringFloat [count] [string] [float] ... [string] [float]
    internal class PushNOfStringFloat : PeepholeOptimization {
        public int GetLength() {
            return 2;
        }

        public List<DreamProcOpcode> GetOpcodes() {
            return new() {
                DreamProcOpcode.PushStringFloat,
                DreamProcOpcode.PushStringFloat
            };
        }

        public void Apply(List<IAnnotatedBytecode> input, int index) {
            int count = 0;
            int stackDelta = 0;
            while (index + count < input.Count && input[index + count] is AnnotatedBytecodeInstruction instruction &&
                   instruction.Opcode == DreamProcOpcode.PushStringFloat) {
                count++;
            }

            var range = input.GetRange(index, count).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            List<IAnnotatedBytecode> args = new();
            args.Add(new AnnotatedBytecodeInteger(count, new Location()));
            for (int i = 0; i < count; i++) {
                args.Add(range[i].GetArgs()[0]);
                args.Add(range[i].GetArgs()[1]);
                stackDelta += 2;
            }

            input.RemoveRange(index, count);
            input.Insert(index,
                new AnnotatedBytecodeInstruction(DreamProcOpcode.PushNOfStringFloats, stackDelta, args));
        }
    }

    // PushResource [resource]
    // ...
    // PushResource [resource]
    // -> PushNResources [count] [resource] ... [resource]
    internal class PushNResources : PeepholeOptimization {
        public int GetLength() {
            return 2;
        }

        public List<DreamProcOpcode> GetOpcodes() {
            return new() {
                DreamProcOpcode.PushResource,
                DreamProcOpcode.PushResource
            };
        }

        public void Apply(List<IAnnotatedBytecode> input, int index) {
            int count = 0;
            int stackDelta = 0;
            while (index + count < input.Count && input[index + count] is AnnotatedBytecodeInstruction instruction &&
                   instruction.Opcode == DreamProcOpcode.PushResource) {
                count++;
            }

            var range = input.GetRange(index, count).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            List<IAnnotatedBytecode> args = new();
            args.Add(new AnnotatedBytecodeInteger(count, new Location()));
            for (int i = 0; i < count; i++) {
                args.Add(range[i].GetArgs()[0]);
                stackDelta++;
            }

            input.RemoveRange(index, count);
            input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.PushNResources, stackDelta, args));
        }
    }

    // PushNFloats [count] [float] ... [float]
    // CreateList [count]
    // -> CreateListNFloats [count] [float] ... [float]
    internal class CreateListNFloats : PeepholeOptimization {
        public int GetLength() {
            return 2;
        }

        public List<DreamProcOpcode> GetOpcodes() {
            return new() {
                DreamProcOpcode.PushNFloats,
                DreamProcOpcode.CreateList
            };
        }

        public bool CheckPreconditions(List<IAnnotatedBytecode> input, int index) {
            var range = input.GetRange(index, 2).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            int pushVal1 = ((range[0].GetArgs()[0] as AnnotatedBytecodeInteger)!).Value;
            int pushVal2 = ((range[1].GetArgs()[0] as AnnotatedBytecodeListSize)!).Size;
            return pushVal1 == pushVal2;
        }

        public void Apply(List<IAnnotatedBytecode> input, int index) {
            var range = input.GetRange(index, 2).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            int pushVal1 = ((range[0].GetArgs()[0] as AnnotatedBytecodeInteger)!).Value;
            List<IAnnotatedBytecode> args = new();
            args.Add(new AnnotatedBytecodeInteger(pushVal1, new Location()));
            args = args.Concat(range[0].GetArgs().GetRange(1, pushVal1)).ToList();
            input.RemoveRange(index, 2);
            input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.CreateListNFloats, 1, args));
        }
    }

    // PushNStrings [count] [string] ... [string]
    // CreateList [count]
    // -> CreateListNStrings [count] [string] ... [string]
    internal class CreateListNStrings : PeepholeOptimization {
        public int GetLength() {
            return 2;
        }

        public List<DreamProcOpcode> GetOpcodes() {
            return new() {
                DreamProcOpcode.PushNStrings,
                DreamProcOpcode.CreateList
            };
        }

        public bool CheckPreconditions(List<IAnnotatedBytecode> input, int index) {
            var range = input.GetRange(index, 2).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            int pushVal1 = ((range[0].GetArgs()[0] as AnnotatedBytecodeInteger)!).Value;
            int pushVal2 = ((range[1].GetArgs()[0] as AnnotatedBytecodeListSize)!).Size;
            return pushVal1 == pushVal2;
        }

        public void Apply(List<IAnnotatedBytecode> input, int index) {
            var range = input.GetRange(index, 2).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            int pushVal1 = ((range[0].GetArgs()[0] as AnnotatedBytecodeInteger)!).Value;
            List<IAnnotatedBytecode> args = new();
            args.Add(new AnnotatedBytecodeInteger(pushVal1, new Location()));
            args = args.Concat(range[0].GetArgs().GetRange(1, pushVal1)).ToList();
            input.RemoveRange(index, 2);
            input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.CreateListNStrings, 1, args));
        }
    }

    // PushNResources [count] [resource] ... [resource]
    // CreateList [count]
    // -> CreateListNResources [count] [resource] ... [resource]
    internal class CreateListNResources : PeepholeOptimization {
        public int GetLength() {
            return 2;
        }

        public List<DreamProcOpcode> GetOpcodes() {
            return new() {
                DreamProcOpcode.PushNResources,
                DreamProcOpcode.CreateList
            };
        }

        public bool CheckPreconditions(List<IAnnotatedBytecode> input, int index) {
            var range = input.GetRange(index, 2).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            int pushVal1 = ((range[0].GetArgs()[0] as AnnotatedBytecodeInteger)!).Value;
            int pushVal2 = ((range[1].GetArgs()[0] as AnnotatedBytecodeListSize)!).Size;
            return pushVal1 == pushVal2;
        }

        public void Apply(List<IAnnotatedBytecode> input, int index) {
            var range = input.GetRange(index, 2).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            int pushVal1 = ((range[0].GetArgs()[0] as AnnotatedBytecodeInteger)!).Value;
            List<IAnnotatedBytecode> args = new();
            args.Add(new AnnotatedBytecodeInteger(pushVal1, new Location()));
            args = args.Concat(range[0].GetArgs().GetRange(1, pushVal1)).ToList();
            input.RemoveRange(index, 2);
            input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.CreateListNResources, 1, args));
        }
    }

    // PushNRefs [count] [ref] ... [ref]
    // CreateList [count]
    // -> CreateListNRefs [count] [ref] ... [ref]
    internal class CreateListNRefs : PeepholeOptimization {
        public int GetLength() {
            return 2;
        }

        public List<DreamProcOpcode> GetOpcodes() {
            return new() {
                DreamProcOpcode.PushNRefs,
                DreamProcOpcode.CreateList
            };
        }

        public bool CheckPreconditions(List<IAnnotatedBytecode> input, int index) {
            var range = input.GetRange(index, 2).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            int pushVal1 = ((range[0].GetArgs()[0] as AnnotatedBytecodeInteger)!).Value;
            int pushVal2 = ((range[1].GetArgs()[0] as AnnotatedBytecodeListSize)!).Size;
            return pushVal1 == pushVal2;
        }

        public void Apply(List<IAnnotatedBytecode> input, int index) {
            var range = input.GetRange(index, 2).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            int pushVal1 = ((range[0].GetArgs()[0] as AnnotatedBytecodeInteger)!).Value;
            List<IAnnotatedBytecode> args = new();
            args.Add(new AnnotatedBytecodeInteger(pushVal1, new Location()));
            args = args.Concat(range[0].GetArgs().GetRange(1, pushVal1)).ToList();
            input.RemoveRange(index, 2);
            input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.CreateListNRefs, 1, args));
        }
    }

    // Jump [label1]
    // Jump [label2] <- Dead code
    // -> Jump [label1]
    internal class RemoveJumpFollowedByJump : PeepholeOptimization {
        public int GetLength() {
            return 2;
        }

        public List<DreamProcOpcode> GetOpcodes() {
            return new() {
                DreamProcOpcode.Jump,
                DreamProcOpcode.Jump
            };
        }

        public void Apply(List<IAnnotatedBytecode> input, int index) {
            input.RemoveAt(index + 1);
        }
    }

    // PushType [type]
    // IsType
    // -> IsTypeDirect [type]
    internal class IsTypeDirect : PeepholeOptimization {
        public int GetLength() {
            return 2;
        }

        public List<DreamProcOpcode> GetOpcodes() {
            return new() {
                DreamProcOpcode.PushType,
                DreamProcOpcode.IsType
            };
        }

        public void Apply(List<IAnnotatedBytecode> input, int index) {
            var range = input.GetRange(index, 2).Select(x => (AnnotatedBytecodeInstruction)x).ToList();
            AnnotatedBytecodeTypeID? pushVal = (range[0].GetArgs()[0] as AnnotatedBytecodeTypeID);
            input.RemoveRange(index, 2);
            input.Insert(index,
                new AnnotatedBytecodeInstruction(DreamProcOpcode.IsTypeDirect,
                    new List<IAnnotatedBytecode> { pushVal }));
        }
    }


    class PeepholeOptimizer {
        private static List<PeepholeOptimization> optimizations = new() {
            new AssignPop(),
            new PushField(),
            new PushNRef(),
            new PushNFloats(),
            new PushNStrings(),
            new PushNResources(),
            new PushNOfStringFloat(),
            new CreateListNFloats(),
            new CreateListNStrings(),
            new CreateListNResources(),
            new CreateListNRefs(),
            new BooleanNotJump(),
            new JumpIfReferenceFalse(),
            new PushStringFloat(),
            new SwitchOnFloat(),
            new SwitchOnString(),
            new RemoveJumpFollowedByJump(),
            new IsTypeDirect(),
            new AssignNull()
        };

        public static void RunPeephole(List<IAnnotatedBytecode> input) {
            int changes = 1;
            while (changes > 0) {
                changes = 0;
                // Pass 1: Find all 5-byte sequences
                for (int i = 0; i < input.Count; i++) {
                    if (i > input.Count - 5) {
                        break;
                    }

                    if (input.GetRange(i, 5).TrueForAll(x => x is AnnotatedBytecodeInstruction)) {
                        var slice5 = input.GetRange(i, 5).Select(x => ((AnnotatedBytecodeInstruction)x).Opcode)
                            .ToList();
                        foreach (var opt in optimizations) {
                            if (opt.GetLength() == 5 && opt.GetOpcodes().SequenceEqual(slice5) &&
                                opt.CheckPreconditions(input, i)) {
                                var startpoint = input.GetRange(i, 5).Where(x => x.GetLocation().Line != null ).FirstOrDefault() ?? input[i];
                                changes++;
                                opt.Apply(input, i);
                                input[i].SetLocation(startpoint);
                            }
                        }
                    }
                }

                // Pass 2: Find all 4-byte sequences
                for (int i = 0; i < input.Count; i++) {
                    if (i > input.Count - 4) {
                        break;
                    }

                    if (input.GetRange(i, 4).TrueForAll(x => x is AnnotatedBytecodeInstruction)) {
                        var slice4 = input.GetRange(i, 4).Select(x => ((AnnotatedBytecodeInstruction)x).Opcode)
                            .ToList();
                        foreach (var opt in optimizations) {
                            if (opt.GetLength() == 4 && opt.GetOpcodes().SequenceEqual(slice4) &&
                                opt.CheckPreconditions(input, i)) {
                                var startpoint = input.GetRange(i, 4).Where(x => x.GetLocation().Line != null).FirstOrDefault() ?? input[i];
                                changes++;
                                opt.Apply(input, i);
                                input[i].SetLocation(startpoint);
                            }
                        }
                    }
                }

                // Pass 3: Find all 3-byte sequences
                for (int i = 0; i < input.Count; i++) {
                    if (i > input.Count - 3) {
                        break;
                    }

                    if (input.GetRange(i, 3).TrueForAll(x => x is AnnotatedBytecodeInstruction)) {
                        var slice3 = input.GetRange(i, 3).Select(x => ((AnnotatedBytecodeInstruction)x).Opcode)
                            .ToList();
                        var slice2 = input.GetRange(i, 2).Select(x => ((AnnotatedBytecodeInstruction)x).Opcode)
                            .ToList();
                        foreach (var opt in optimizations) {
                            if (opt.GetLength() == 3 && opt.GetOpcodes().SequenceEqual(slice3) &&
                                opt.CheckPreconditions(input, i)) {
                                var startpoint = input.GetRange(i, 3).Where(x => x.GetLocation().Line != null).FirstOrDefault() ?? input[i];
                                changes++;
                                opt.Apply(input, i);
                                input[i].SetLocation(startpoint);
                            }
                        }
                    }
                }

                // Pass 4: Find all 2-byte sequences
                for (int i = 0; i < input.Count; i++) {
                    if (i > input.Count - 2) {
                        break;
                    }

                    if (input.GetRange(i, 2).TrueForAll(x => x is AnnotatedBytecodeInstruction)) {
                        var slice2 = input.GetRange(i, 2).Select(x => ((AnnotatedBytecodeInstruction)x).Opcode)
                            .ToList();
                        foreach (var opt in optimizations) {
                            if (opt.GetLength() == 2 && opt.GetOpcodes().SequenceEqual(slice2) &&
                                opt.CheckPreconditions(input, i)) {
                                changes++;
                                var startpoint = input.GetRange(i, 2).Where(x => x.GetLocation().Line != null).FirstOrDefault() ?? input[i];
                                opt.Apply(input, i);
                                input[i].SetLocation(startpoint);
                            }
                        }
                    }
                }
            }
        }
    }
}
