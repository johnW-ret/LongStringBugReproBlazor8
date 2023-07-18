using Microsoft.AspNetCore.Components;

namespace BlazorApp4.Pages;

public partial class LongString : ComponentBase
{
    public string __Text { get; set; } = """
# Pattern Matching in C#
## <time datetime="2023-07-18"></time>
        
My original reason for writing this post was that, some time ago, I felt that the Microsoft documentation for pattern matching in C# did not fully communicate all the cool things you could do with pattern matching in everyday code. Since then, I have come across [this very succinctly summary of Pattern matching](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/pattern-matching) from Microsoft. However, I thought that this post could still serve benefit as an introduction in using pattern matching to make everyday code more concise.
        
Pattern matching, in short, is **a way you can test a value to see if it has certain characteristics**. In many languages, you can write the tests as *descriptions* of the object under test. Consider the below example:
        
```csharp
list is [0, 1, >3]
```
        
We haven't covered this specific syntax yet, but I can tell you this code resolves to either `true` or `false`. It could reasonably be inferred that the code checks the first two elements of `list` are `0` and `1`, and perhaps, that the third element is greater than `3`. Less noticably, it is also inferred that the `Count` of the `list` is `3`.
        
Patterns often let you extract data directly using the syntax of the pattern. Consider the below example.
        
```csharp
list is [0, 1, >3, int number, 4]
```
        
The above code (or *expression*) checks the first three elements like the prior example, but in addition to ensuring a `Count` of *five* elements, we now see an `int number` snuggled between the `>3` and the `4`.
        
The above code still resolves to `true` or `false`. The `list` may or may not match the pattern. However, if it does, our `int number` will now contain the element in between `>3` and `4`. This is what is meant above by using patterns to "extract data".
        
I should note here that the examples and styles presented in this article reflect my personal preference, and that I am not proposing an objective standard on a "better" way to write code.
        
# Nullable Reference Types
Firstly, to properly explain the benefit of pattern matching operators, I have to briefly mention Nullable Reference Types.
        
C# has had nullable value types (the struct ``Nullable<T>``) since C# 2.0, which serve as wrappers for value types which can hold ``null``. ``Nullable<T>``s are denoted by appending ``?`` to a value type, like so:
        
```csharp
public int Age { get; set; } // value type
public int? MaybeAge { get; set; } // nullable value type (Nullable<int>)
```
        
Nullable Reference Types (or NRTs, as they are called) are denoted by the same syntax of ``?``. But you may be confused as to why they exist, as reference types are innately nullable in C#. NRTs exist to **communicate that ``null`` is a valid value for a variable**. On the contrary, this communicates that ``null`` is **not** a valid value (or state, if you will) for variables of *non*-NRTs (so normal reference types). There have always been debates as to whether ``null`` should be accepted as a valid state for a variable, and null reference errors have been the source of countless bugs and hours of frustration in almost all programming languages since ``null``'s invention.
        
NRT-enabled projects make the validity of a "``null`` state" part of the variable's type. More than that, the developer actually gets some goodies from the compiler for giving it this extra information. With NRTs enabled, by default, the C# compiler will ensure that - barring ``System.Reflection``, Json deserialization, and cataclysmic world catastrophes - non-NRTs will be guaranteed to be ``null`` for the duration of the program.
        
```csharp
string name = null; // immediate warning, object is null by default, CS8600	Converting null literal or possible null value to non-nullable type.
string? maybeString = null;
```
        
As such, if the developer tries to "dereference" (use) a variable without having ensured it is not ``null``, [the compiler emits a warning](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/nullable-warnings).
        
```csharp
name = maybeString; // emits CS8600
        
if (maybeString != null)
    name = maybeString; // ✅ OK
```
        
It is worth noting here, that all null state checks associated with enabling NRTs are static and do not add runtime checks, which is why things like ``System.Reflection`` can invalidate the state.
        
The beauty of NRT is that nullable state can become part of your API definitions. Instead of peppering you code with repetitive null checks which can degrade performance and readability, you can define a point between your front-end and back-end precisely where `null`'s must be discarded, which means no more null checks and `ArgumentNullException.ThrowIfNull`s unless you have a strong reason to not trust the static analysis.
        
Fair warning: if this seems boring to you, the facets of pattern matching operators I will try to emphasize here might seem underwhelming. However, if this is a feature you'd put a ring on and honeymoon with, this next section should be where it "finally gets good".
        
# ``is`` and ``not``
You may have seen the following syntax before:
```csharp
var @object = GetSomeReferenceType();
        
if (@object is not null)
    ; // do something
```
        
This syntax obviously feels a lot like ``!=``, so the clever mind may start substituting ``null`` for other values on the right. They'd quickly find that this does not work for non-const variables, emitting a CS0150.
        
```csharp
record Person(string Name, int Age, Person? BestFriend);
        
Person kelly = new("Kelly", 40, null);
var person = GetSomePerson();
        
if (kelly is person) // CS0150: A constant value is expected
    ; // do something
```
        
I have received a similar error which essentially says the right-side of a pattern matching expression must be ``const`` *or an l-value*. While as of this writing, I have not been able to replicate this error message, I found that introducing the term l-value is informative as to how pattern matching expressions work.
        
An l-value stands for a "locator value" and generally refers to a variable declaration. That means anything (double-check me on this) that can be used to declare a variable can be used in a pattern matching expression.
        
```csharp
if (maybePerson is Person person)
    ; // do something
        
// or
        
if (maybePerson is var person)
    ; // do something
```
        
This syntax conveniently moves null-checking and assignment into a single expression.
        
```csharp
if (maybePerson != null)
    Person person = maybePerson;
        
// or
        
Person person;
if (maybePerson != null)
    person = maybePerson;
        
// vs
        
if (maybePerson is Person person)
    ; // ...
```
        
The null-state static analysis ensures that the scope of the variable matches what can be ensured by the logic of the code. For example, ``person`` is not valid outside of the ``if`` statement, since that is the only place it is guaranteed to not be ``null``.
        
```csharp
if (maybePerson is Person person)
    ; // ✅ use person here
        
// ❌ person is not valid outside of scope
```
        
What's really nice, is that that this pattern works with guard clauses too.
        
```csharp
if (maybePerson is not Person person)
    return;
        
// can use person here
```
        
This code might look a bit weird at first, since we expect a variable defined in an ``if`` statement (like an ``out`` parameter) to be valid within the scope of the corresponding block, but in the above example, ``Person person`` is actually being defined in the *outside* scope only. What we're really saying is "if this maybe-``null`` variable `maybePerson` is ``not null`` (it *does* conform to the non-NRT type), assign it to ``person`` and move on".
        
As David Fowler [pointed out](https://twitter.com/davidfowl/status/1606503770992832513), pattern matching (can) move assignment to the right.
        
You may have been thinking, "I use ``var`` a lot... can I use ``var`` with pattern matching?". And the answer is: you sure can! Though it may not be as useful as you think, since ``var`` corresponds to the *nullable* version of the inferred type.
        
```csharp
_ = person is var alsoPerson;
        
// translates to
        
_ = person is Person? alsoPerson;
```
        
The `null`-check comes from testing against the non-nullable type, which is only possible (as far as I am aware) when writing out the whole type name and not `var`. There is, however, a way ``var`` could be useful for property patterns, which are covered below.
        
# Multiple Inputs, List Patterns and ``when``
        
So far we have been using pattern matching to add some flair to our null checks - which I admit is the bulk of how I use pattern matching syntax these days. However, pattern matching itself is much more powerful and akin to [``match`` in F#](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/match-expressions) - which I will not get into as it would be a detour and I do not feel confident enough in F# to expound upon it.
        
The first way I have started using pattern matching in this section is with multiple inputs, something I like to call "bracket notation".
        
I like to describe pattern matching with `if` statements as defining the object you want to receive, then testing and assigning that object in one go according to your description, as opposed to testing a number of conditions on a variable and choosing to assign or discard it. Note the difference in the following case:
        
```csharp
Person person = new("Ken", 25, null);
        
if (person.Name == "Ken" && person.Age >= 18)
    ; // do something
        
// vs
        
if (person is { Name: "Ken", Age: >=18 })
    ; // do something
```
        
You can note that both of these examples both do essentially the same thing, but the pattern matching example is literally shorter and arguably more concise.
        
This bracket notation lets you describe what you want your input to conform to, instead of performing a series of Boolean checks. When you have a lot of properties and fields you want to check, this can - again, arguably - result in cleaner code.
        
Perhaps you are at the edge of your seat on this syntax. However, it is worth revisiting the non-`const` restriction above again, as the bulk of `if` checks performed are against non-`const` variables. Note the code below does not compile:
        
```csharp
int MinimumAge = 18; // non-const!
        
if (person is {Name: "Ken"} and {Age: >=MinimumAge}) // error CS0150: A constant value is expected
    ; // do something
```
        
Pattern matching expressions must conform to one of the patterns in [this C# patterns reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/patterns).
        
**A variable is not a pattern**, so the correct version of the code above would be the following:
        
```csharp
int MinimumAge = 18;
        
if (person is {Name: "Ken"} and {Age: int age} && age >= MinimumAge)
    ; // do something
```
        
You may be thinking that this defeats the purpose of using a pattern matching expression in this instance, and you may be right.
        
 other hand, you may find that making checks against several properties on an object with several layers (or nodes in a tree) of hierarchy may be convenient - particularly by using nested property patterns.
        
```csharp
if (person is { BestFriend: { Name: "Bob" } })
    ; // do something if person has a BestFriend named Bob
```
        
Take the below example, where we check if a `Node` on a tree has a certain structure:
        
```csharp
class Node
{
    public Node? Left { get; set; }
    public Node? Right { get; set; }
    public string Value { get; set; }
}
        ...

""";

    public string Dots { get; set; }
        =
"""
........................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................
........................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................
........................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................
........................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................
........................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................
........................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................
........................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................
........................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................
........................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................
........................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................
..........................................................................................
........................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................
........................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................
........................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................
........................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................
.................................................................................................................................................
""";

    protected override void OnInitialized()
    {
        System.Diagnostics.Debug.WriteLine(__Text.Length * sizeof(char));
        System.Diagnostics.Debug.WriteLine(Dots.Length * sizeof(char));
    }
}
