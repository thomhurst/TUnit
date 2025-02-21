namespace TUnit.TestProject.ComplexDependsOn;

[InheritsTests]
public class Tests : BaseClass;

[InheritsTests]
[DependsOn<Tests>]
public class ReadTests : BaseClass;

[InheritsTests]
[DependsOn<ReadTests>]
public class UpdateTests : BaseClass;

[InheritsTests]
[DependsOn<ReadTests>]
[DependsOn<UpdateTests>]
public class DeleteTests : BaseClass;

[InheritsTests]
[DependsOn<Tests>]
public class CreateTests2 : BaseClass;

[InheritsTests]
[DependsOn<CreateTests2>]
public class ReadTests2 : BaseClass;

[InheritsTests]
[DependsOn<ReadTests2>]
public class UpdateTests2 : BaseClass;

[InheritsTests]
[DependsOn<ReadTests2>]
[DependsOn<UpdateTests2>]
public class DeleteTest2 : BaseClass;