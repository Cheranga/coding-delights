# Making CSharpier Husky

## Notes

* Creating a tool manifest

`dotnet new tool-manifest`

* Installing CSharpier as a tool

`dotnet tool install csharpier`

* Installing Husky

`dotnet tool install husky`
`dotnet husky install`

* Adding a pre-commit hook

`dotnet husky add pre-commit -c "echo 'Hi Husky!'"`

After adding this a file is created with the hook name. In this case pre-commit.
In here you can write the command that you want to run before commiting.
A better approach is to organize the tasks in the tasj-runner.json file.
This is used by Husky.NET to run the tasks.

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

* There are two tasks, Run CSharpier and Conventional Commit Linter.
* The Run CSharpier belongs to the group pre-commit, and in the pre-commit hook we have configured to run this task

```shell
#!/bin/sh
. "$(dirname "$0")/_/husky.sh"

dotnet husky run --group pre-commit
```

So if you need to run another task before you commit changes, add it to the task-runner.json
with the group pre-commit.

* The Conventional Commit Linter belongs to the group commit-msg, and in the commit-msg hook we have configured to run
  this task

```shell
#!/bin/sh
. "$(dirname "$0")/_/husky.sh"

dotnet husky run --group commit-msg -a $1
```

In here it passes the first argument to the tasks which belong in the commit-msg group.
Then this argument is passed into the commit-msg.sh script.
In here we evaluate the commit message to see if it follows the conventional commit message format.

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

## Installing CSharpier

### Configuring CSharpier

## Installing Husky

### Configuring Husky

## Using them together

## Enforcing to run before commiting

### Git Hooks

### Configuring CSharpier to run before commiting
