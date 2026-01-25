using Microsoft.VisualStudio.TestTools.UnitTesting;

// Configure MSTest behavior - run tests in parallel at method level
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
