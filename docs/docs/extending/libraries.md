# Libraries

If you want a library package to define things like re-useable base classes with hooks etc, then you shouldn't use the main `TUnit` package - As this assumes your project is a test project and tries to build it as an executable etc.

Instead, reference `TUnit.Core` instead - It has all of the models required for wiring up your tests, but without all the extra setting up of the test suite execution.
