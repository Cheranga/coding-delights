# Using Husky with CSharpier

## Context
This article is about using Husky to automate the enforcing of

:white_check_mark: Code formatting before commiting changes

:white_check_mark:Enforcing commit message pattern

## What is Husky.NET?

[Husky.Net](https://alirezanet.github.io/Husky.Net/guide/#features) is a tool which can be used to run tasks integrated with git hooks.
For the purpose of this article, we will use Husky to run tasks in the `pre-commit` and `commit-msg` hooks.

:white_check_mark: In the `pre-commit` hook, we will run CSharpier to format the C# code before commiting the changes.

:white_check_mark: In the `commit-msg` hook, we will run a script to check if the commit message follows the conventional 
commit message format.

## What is CSharpier and why do we need it?

[CSharpier](https://csharpier.com/docs/About) is an opinionated code formatter for C#.
It is a tool that can be used to format the C# code in a consistent way.

Developers share different IDEs and editors, and each of them has its own code formatting rules.
Also developers have their personal preferences as well.

But when working in a team, it is important to have a consistent code style in your code, and use it throughout
your repositories.

## Why not StyleCop?
Simply StyleCop does too many things in my opinion.
After the more mature analyzers introduced by Microsoft and other open source community, I think 
we can finally separate code analyzers and code formatters.

Use code analyzers for code quality and a formatting tool such as CSharpier for code formatting.

## Installing CSharpier

`CSharpier` is implemented as a dotnet tool.

* If you dont any dotnet tools configured, create a tool manifest

`dotnet new tool-manifest`

* Installing CSharpier as a tool

`dotnet tool install csharpier`

* CSharpier is ready to use now

Running the below command from your root directory will format, all the C# files.

`dotnet csharpier .`

* Configuring CSharpier for your preferences

`CSharpier` can be configured using any of the below configuration files.

:white_check_mark: `.csharpierrc` file in `JSON` or `YAML`

:white_check_mark: `.csharpierrc.json` file or `.csharpierrc.yaml` file

:white_check_mark: `.editorconfig` file

The preference will be given to the `.csharpierrc` file based on the location of the file being formatted.

[Read the docs for more details](https://csharpier.com/docs/Configuration)

## Installing Husky.Net

`Husky.Net` is also implemented as a dotnet tool.

* Installing Husky.Net as a dotnet tool

`dotnet tool install husky`

* Integrate git hooks with Husky.Net

`dotnet husky install`

* Adding a pre-commit hook

`dotnet husky add pre-commit -c "echo 'Hi Husky!'"`

* Adding a commit-msg hook

`dotnet husky add commit-msg -c "echo 'Hi Husky!'"`

After running these commands respective files are created with the hook name.
In this file you can write the command to execute.
There can be many tasks which you might need to execute for a single hook.

So a better approach is to organize the tasks in the `task-runner.json` file.

* Configuring task-runner.json

```json
{
  "$schema": "https://alirezanet.github.io/Husky.Net/schema.json",
  "tasks": [
    {
      "name": "Run CSharpier",
      "group": "pre-commit",
      "command": "dotnet",
      "args": [
        "csharpier",
        "."
      ]
    },
    {
      "name": "Conventional Commit Linter",
      "command": "bash",
      "group": "commit-msg",
      "args": [
        ".husky/commit-msg.sh",
        "${args}"
      ]
    }
  ]
}
```

There are two tasks, `Run CSharpier` and `Conventional Commit Linter`.
As you can see both these tasks belong to the groups `pre-commit` and `commit-msg` respectively.

* Configuring the pre-commit hook

Edit the `pre-commit` file to run the tasks in the `pre-commit` group.

```shell
#!/bin/sh
. "$(dirname "$0")/_/husky.sh"

dotnet husky run --group pre-commit
```

Edit the `commit-msg` file to run the tasks in the `commit-msg` group.

```shell
#!/bin/sh
. "$(dirname "$0")/_/husky.sh"

dotnet husky run --group commit-msg -a $1
```

```shell
#!/bin/sh
. "$(dirname "$0")/_/husky.sh"

dotnet husky run --group pre-commit
```

I am using a commit standard called [conventional commits](https://www.conventionalcommits.org/en/v1.0.0/).

In here I am using a bash script to be executed in the `commit-msg` hook

```shell
#!/usr/bin/env bash

requiredPattern="^(build|[Cc]hore|ci|docs|feat|fix|perf|refactor|revert|style|test|Publish)(\([a-zA-Z]{3,}\-[0-9]
{1,7}\))?: .*"

error_msg='
❌ Commit message does not follow the Conventional Commit standard!

Examples of valid commit messages are,
✅ feat(auth): add login functionality
✅ fix(user-profile): fix avatar upload issue
✅ docs: update API documentation
'

commit_msg=$(cat $1)

echo "Commiting changes with....." >&2
echo "${commit_msg}" >&2

if ! [[ $commit_msg =~ $requiredPattern ]];
then
echo "${error_msg}" >&2
echo "Commit message input: "
echo "${commit_msg}"
exit 1
fi
```

* Using C# code in your git hooks

Best thing is you can write C# scripts and use them in your git hooks.

I have included the same script as in the [documentation](https://alirezanet.github.io/Husky.Net/guide/csharp-script.html)
 as an example.
