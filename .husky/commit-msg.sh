#!/usr/bin/env bash

requiredPattern="^(build|[Cc]hore|ci|docs|feat|fix|perf|refactor|revert|style|test|Publish)(\([a-zA-Z]{3,}\-[0-9]{1,7}\))?: .*"

error_msg='
❌   Commit message does not follow the Conventional Commit standard!

Examples of valid commit messages are,
✅   feat(auth): add login functionality
✅   fix(user-profile): fix avatar upload issue
✅   docs: update API documentation
'

commit_msg=$(cat $1)

if ! [[ $commit_msg =~ $requiredPattern ]];
then
  echo "${error_msg}" >&2
  echo "Commit message input: "
  echo "${commit_msg}"
  exit 1
fi