#!/bin/bash
# Script to push the restructure branch and create a pull request

set -e

echo "=========================================="
echo "Pushing restructure/canonical-layout branch"
echo "=========================================="

# Push the branch
git push -u origin restructure/canonical-layout

echo ""
echo "=========================================="
echo "Creating Pull Request"
echo "=========================================="

# Check if gh CLI is available
if command -v gh &> /dev/null; then
    echo "Using GitHub CLI to create PR..."

    # Read the PR description from the file
    PR_BODY=$(cat PR-DESCRIPTION.md)

    # Create the PR
    gh pr create \
        --title "Reorganize RAG-Sandbox to canonical layout" \
        --body "$PR_BODY" \
        --base main \
        --head restructure/canonical-layout

    echo ""
    echo "✅ Pull request created successfully!"
    echo "Use 'gh pr view --web' to open it in your browser"
else
    echo "GitHub CLI (gh) not found."
    echo ""
    echo "To create the PR manually:"
    echo "1. Go to: https://github.com/krunchyMonkey/RAG-Sandbox/compare/main...restructure/canonical-layout"
    echo "2. Click 'Create pull request'"
    echo "3. Copy the content from PR-DESCRIPTION.md as the PR body"
    echo ""
    echo "Or install GitHub CLI: https://cli.github.com/"
fi

echo ""
echo "=========================================="
echo "Summary"
echo "=========================================="
echo "Branch: restructure/canonical-layout"
echo "Commits:"
git log --oneline main..restructure/canonical-layout
echo ""
echo "PR Description available in: PR-DESCRIPTION.md"
