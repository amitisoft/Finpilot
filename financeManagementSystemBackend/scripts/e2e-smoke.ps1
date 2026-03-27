param(
    [string]$BaseUrl = 'http://localhost:5000'
)

$ErrorActionPreference = 'Stop'

function Assert-True {
    param(
        [object]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw "ASSERT FAILED: $Message"
    }
}

function Invoke-Api {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Path,
        $Body,
        [string]$Token
    )

    $headers = @{}
    if ($Token) {
        $headers['Authorization'] = "Bearer $Token"
    }

    $params = @{
        Method = $Method
        Uri = "$BaseUrl$Path"
        Headers = $headers
        ContentType = 'application/json'
    }

    if ($PSBoundParameters.ContainsKey('Body')) {
        $params.Body = ($Body | ConvertTo-Json -Depth 10)
    }

    return Invoke-RestMethod @params
}


function Wait-ForAgentResult {
    param(
        [Parameter(Mandatory = $true)][Guid]$TransactionId,
        [Parameter(Mandatory = $true)][string]$Token,
        [int]$MaxAttempts = 12
    )

    for ($attempt = 0; $attempt -lt $MaxAttempts; $attempt++) {
        try {
            $result = Invoke-Api -Method Get -Path "/api/agents/results/transactions/$TransactionId" -Token $Token
            if ($result.success -and $result.data.status -ne 'queued') {
                return $result
            }
        }
        catch {
        }

        Start-Sleep -Seconds 1
    }

    throw "Timed out waiting for agent result for transaction $TransactionId"
}

function Wait-ForBudgetAgentResult {
    param(
        [Parameter(Mandatory = $true)][Guid]$BudgetId,
        [Parameter(Mandatory = $true)][string]$Token,
        [int]$MaxAttempts = 12
    )

    for ($attempt = 0; $attempt -lt $MaxAttempts; $attempt++) {
        $results = Invoke-Api -Method Get -Path "/api/agents/results?agent=2&sourceEntityId=$BudgetId" -Token $Token
        if ($results.success -and $results.data.Count -ge 1) {
            $latest = $results.data | Select-Object -First 1
            if ($latest.status -ne 'queued') {
                return $latest
            }
        }

        Start-Sleep -Seconds 1
    }

    throw "Timed out waiting for budget agent result for budget $BudgetId"
}

$timestamp = Get-Date -Format 'yyyyMMddHHmmss'
$email = "e2e_$timestamp@example.com"
$password = 'Password@123'
$now = Get-Date
$currentMonth = $now.Month
$currentYear = $now.Year
$nextYear = $now.AddYears(1)

$results = [System.Collections.Generic.List[string]]::new()

# Health
$health = Invoke-RestMethod -Method Get -Uri "$BaseUrl/health"
Assert-True ($health -eq 'Healthy') 'Health endpoint should return Healthy.'
$results.Add('Health endpoint')

# Register/Login/Me
$register = Invoke-Api -Method Post -Path '/api/Auth/register' -Body @{
    fullName = 'E2E Test User'
    email = $email
    password = $password
}
Assert-True $register.success 'Register should succeed.'
$token = $register.data.accessToken
Assert-True (-not [string]::IsNullOrWhiteSpace($token)) 'Register should return an access token.'
$results.Add('Auth register')

$login = Invoke-Api -Method Post -Path '/api/Auth/login' -Body @{
    email = $email
    password = $password
}
Assert-True $login.success 'Login should succeed.'
$token = $login.data.accessToken
Assert-True ($login.data.accessTokenExpiresAt) 'Login should return access token expiry.'
$results.Add('Auth login')

$me = Invoke-Api -Method Get -Path '/api/Auth/me' -Token $token
Assert-True ($me.success -and $me.data.email -eq $email) 'Me should return the logged in user.'
$results.Add('Auth me')

# Categories
$categories = Invoke-Api -Method Get -Path '/api/categories' -Token $token
Assert-True ($categories.success -and $categories.data.Count -ge 2) 'Categories list should contain seeded categories.'
$expenseCategory = $categories.data | Where-Object { $_.type -eq 2 -and $_.isDefault } | Select-Object -First 1
$incomeCategory = $categories.data | Where-Object { $_.type -eq 1 -and $_.isDefault } | Select-Object -First 1
Assert-True ($null -ne $expenseCategory) 'Need a default expense category.'
Assert-True ($null -ne $incomeCategory) 'Need a default income category.'
$results.Add('Categories list')

$customCategory = Invoke-Api -Method Post -Path '/api/categories' -Token $token -Body @{
    name = "Temp Category $timestamp"
    type = 2
    color = '#123456'
    icon = 'tag'
}
Assert-True $customCategory.success 'Create category should succeed.'
$customCategoryId = $customCategory.data.id
$results.Add('Categories create')

$categoryById = Invoke-Api -Method Get -Path "/api/categories/$customCategoryId" -Token $token
Assert-True ($categoryById.success -and $categoryById.data.id -eq $customCategoryId) 'Get category by id should succeed.'
$results.Add('Categories get by id')

$updatedCategory = Invoke-Api -Method Put -Path "/api/categories/$customCategoryId" -Token $token -Body @{
    name = "Temp Category Updated $timestamp"
    type = 2
    color = '#654321'
    icon = 'pencil'
}
Assert-True ($updatedCategory.success -and $updatedCategory.data.name -like 'Temp Category Updated*') 'Update category should succeed.'
$results.Add('Categories update')

$deleteCategory = Invoke-Api -Method Delete -Path "/api/categories/$customCategoryId" -Token $token
Assert-True $deleteCategory.success 'Delete category should succeed.'
$results.Add('Categories delete')

# Accounts
$mainAccount = Invoke-Api -Method Post -Path '/api/accounts' -Token $token -Body @{
    name = 'Primary Bank'
    type = 2
    currency = 'INR'
    openingBalance = 50000
}
Assert-True $mainAccount.success 'Create main account should succeed.'
$mainAccountId = $mainAccount.data.id
$results.Add('Accounts create')

$tempAccount = Invoke-Api -Method Post -Path '/api/accounts' -Token $token -Body @{
    name = 'Temp Wallet'
    type = 4
    currency = 'INR'
    openingBalance = 1000
}
Assert-True $tempAccount.success 'Create temp account should succeed.'
$tempAccountId = $tempAccount.data.id

$accounts = Invoke-Api -Method Get -Path '/api/accounts' -Token $token
Assert-True ($accounts.success -and $accounts.data.Count -ge 2) 'Accounts list should show created accounts.'
$results.Add('Accounts list')

$accountById = Invoke-Api -Method Get -Path "/api/accounts/$mainAccountId" -Token $token
Assert-True ($accountById.success -and $accountById.data.currentBalance -eq 50000) 'Get account by id should reflect opening balance.'
$results.Add('Accounts get by id')

$updatedAccount = Invoke-Api -Method Put -Path "/api/accounts/$mainAccountId" -Token $token -Body @{
    name = 'Primary Bank Updated'
    type = 2
    currency = 'INR'
    openingBalance = 52000
}
Assert-True ($updatedAccount.success -and $updatedAccount.data.openingBalance -eq 52000) 'Update account should succeed.'
$results.Add('Accounts update')

$deleteTempAccount = Invoke-Api -Method Delete -Path "/api/accounts/$tempAccountId" -Token $token
Assert-True $deleteTempAccount.success 'Delete unused account should succeed.'
$results.Add('Accounts delete')

# Transactions
$incomeTxn = Invoke-Api -Method Post -Path '/api/transactions' -Token $token -Body @{
    accountId = $mainAccountId
    categoryId = $incomeCategory.id
    type = 1
    amount = 30000
    description = 'March salary'
    transactionDate = (Get-Date).ToString('o')
    merchant = 'Employer'
    notes = 'Salary credit'
}
Assert-True $incomeTxn.success 'Create income transaction should succeed.'
$incomeTxnId = $incomeTxn.data.id
$results.Add('Transactions create income')

$expenseTxn = Invoke-Api -Method Post -Path '/api/transactions' -Token $token -Body @{
    accountId = $mainAccountId
    categoryId = $expenseCategory.id
    type = 2
    amount = 2500
    description = 'Groceries'
    transactionDate = (Get-Date).ToString('o')
    merchant = 'Store'
    notes = 'Weekly groceries'
}
Assert-True $expenseTxn.success 'Create expense transaction should succeed.'
$expenseTxnId = $expenseTxn.data.id
$results.Add('Transactions create expense')

$transactions = Invoke-Api -Method Get -Path '/api/transactions' -Token $token
Assert-True ($transactions.success -and $transactions.data.Count -ge 2) 'Transactions list should contain created transactions.'
$results.Add('Transactions list')

$transactionById = Invoke-Api -Method Get -Path "/api/transactions/$expenseTxnId" -Token $token
Assert-True ($transactionById.success -and $transactionById.data.amount -eq 2500) 'Get transaction by id should succeed.'
$results.Add('Transactions get by id')

$updatedExpense = Invoke-Api -Method Put -Path "/api/transactions/$expenseTxnId" -Token $token -Body @{
    accountId = $mainAccountId
    categoryId = $expenseCategory.id
    type = 2
    amount = 3000
    description = 'Groceries updated'
    transactionDate = (Get-Date).ToString('o')
    merchant = 'Store'
    notes = 'Updated expense'
}
Assert-True ($updatedExpense.success -and $updatedExpense.data.amount -eq 3000) 'Update transaction should succeed.'
$results.Add('Transactions update')


$queuedAgentResult = Wait-ForAgentResult -TransactionId $expenseTxnId -Token $token
Assert-True ($queuedAgentResult.success -and $queuedAgentResult.data.agent -eq 1) 'Queued anomaly screening should complete successfully.'
Assert-True ($queuedAgentResult.data.anomaly.transactionId -eq $expenseTxnId) 'Queued anomaly result should target the expense transaction.'
$results.Add('Agents queued anomaly screening')

$agentInvoke = Invoke-Api -Method Post -Path '/api/agents/invoke' -Token $token -Body @{
    agent = 1
    trigger = 2
    transactionId = $expenseTxnId
}
Assert-True ($agentInvoke.success -and $agentInvoke.data.result.agent -eq 1) 'Agent invoke should succeed for anomaly checks.'
Assert-True (-not [string]::IsNullOrWhiteSpace($agentInvoke.data.result.anomaly.severity)) 'Agent invoke should return anomaly analysis.'
$results.Add('Agents invoke anomaly')

$agentResults = Invoke-Api -Method Get -Path '/api/agents/results?agent=1' -Token $token
Assert-True ($agentResults.success -and $agentResults.data.Count -ge 1) 'Agent results list should contain anomaly results.'
$results.Add('Agents results list')

$agentDismiss = Invoke-Api -Method Post -Path "/api/agents/results/$($agentInvoke.data.result.id)/dismiss" -Token $token
Assert-True $agentDismiss.success 'Agent dismiss should succeed.'
$results.Add('Agents dismiss result')

# Budgets
$budget = Invoke-Api -Method Post -Path '/api/budgets' -Token $token -Body @{
    name = "Budget $currentMonth/$currentYear"
    month = $currentMonth
    year = $currentYear
    totalLimit = 10000
    alertThresholdPercent = 80
    items = @(
        @{
            categoryId = $expenseCategory.id
            limitAmount = 6000
        }
    )
}
Assert-True $budget.success 'Create budget should succeed.'
$budgetId = $budget.data.id
$results.Add('Budgets create')

$budgets = Invoke-Api -Method Get -Path '/api/budgets' -Token $token
Assert-True ($budgets.success -and $budgets.data.Count -ge 1) 'Budgets list should contain created budget.'
$results.Add('Budgets list')

$budgetById = Invoke-Api -Method Get -Path "/api/budgets/$budgetId" -Token $token
Assert-True ($budgetById.success -and $budgetById.data.totalSpent -ge 3000) 'Get budget by id should reflect spent amount.'
$results.Add('Budgets get by id')

$updatedBudget = Invoke-Api -Method Put -Path "/api/budgets/$budgetId" -Token $token -Body @{
    name = "Budget Updated $currentMonth/$currentYear"
    month = $currentMonth
    year = $currentYear
    totalLimit = 12000
    alertThresholdPercent = 75
    items = @(
        @{
            categoryId = $expenseCategory.id
            limitAmount = 7000
        }
    )
}
Assert-True ($updatedBudget.success -and $updatedBudget.data.totalLimit -eq 12000) 'Update budget should succeed.'
$results.Add('Budgets update')

$budgetStatus = Invoke-Api -Method Get -Path '/api/budgets/status' -Token $token
Assert-True ($budgetStatus.success -and $budgetStatus.data.Count -ge 1) 'Budget status should return at least one item.'
$results.Add('Budgets status')

$queuedBudgetResult = Wait-ForBudgetAgentResult -BudgetId $budgetId -Token $token
Assert-True ($queuedBudgetResult.agent -eq 2) 'Queued budget screening should complete successfully.'
Assert-True (-not [string]::IsNullOrWhiteSpace($queuedBudgetResult.budget.status)) 'Queued budget screening should return budget analysis.'
$results.Add('Agents queued budget screening')

$budgetAgentInvoke = Invoke-Api -Method Post -Path '/api/agents/invoke' -Token $token -Body @{
    agent = 2
    trigger = 2
    budgetId = $budgetId
}
Assert-True ($budgetAgentInvoke.success -and $budgetAgentInvoke.data.result.agent -eq 2) 'Agent invoke should succeed for budget checks.'
Assert-True (-not [string]::IsNullOrWhiteSpace($budgetAgentInvoke.data.result.budget.status)) 'Budget agent invoke should return budget analysis.'
$results.Add('Agents invoke budget advisor')

$coachAgentInvoke = Invoke-Api -Method Post -Path '/api/agents/invoke' -Token $token -Body @{
    agent = 3
    trigger = 2
}
Assert-True ($coachAgentInvoke.success -and $coachAgentInvoke.data.result.agent -eq 3) 'Agent invoke should succeed for coach checks.'
Assert-True ($coachAgentInvoke.data.result.coach.healthScore -ge 25) 'Coach invoke should return a valid health score.'
$results.Add('Agents invoke coach')

$investmentAgentInvoke = Invoke-Api -Method Post -Path '/api/agents/invoke' -Token $token -Body @{
    agent = 4
    trigger = 2
    riskProfile = 'moderate'
    age = 29
}
Assert-True ($investmentAgentInvoke.success -and $investmentAgentInvoke.data.result.agent -eq 4) 'Agent invoke should succeed for investment guidance.'
Assert-True ($investmentAgentInvoke.data.result.investment.allocationSuggestions.Count -ge 1) 'Investment invoke should return allocation suggestions.'
$results.Add('Agents invoke investment advisor')

$reportAgentInvoke = Invoke-Api -Method Post -Path '/api/agents/invoke' -Token $token -Body @{
    agent = 5
    trigger = 2
}
Assert-True ($reportAgentInvoke.success -and $reportAgentInvoke.data.result.agent -eq 5) 'Agent invoke should succeed for report generation.'
Assert-True (-not [string]::IsNullOrWhiteSpace($reportAgentInvoke.data.result.report.markdownReport)) 'Report invoke should return markdown content.'
$results.Add('Agents invoke report generator')

# Goals
$goal = Invoke-Api -Method Post -Path '/api/goals' -Token $token -Body @{
    name = 'Emergency Fund'
    targetAmount = 100000
    currentAmount = 15000
    targetDate = $nextYear.ToString('o')
}
Assert-True $goal.success 'Create goal should succeed.'
$goalId = $goal.data.id
$results.Add('Goals create')

$goals = Invoke-Api -Method Get -Path '/api/goals' -Token $token
Assert-True ($goals.success -and $goals.data.Count -ge 1) 'Goals list should contain created goal.'
$results.Add('Goals list')

$goalById = Invoke-Api -Method Get -Path "/api/goals/$goalId" -Token $token
Assert-True ($goalById.success -and $goalById.data.targetAmount -eq 100000) 'Get goal by id should succeed.'
$results.Add('Goals get by id')

$updatedGoal = Invoke-Api -Method Put -Path "/api/goals/$goalId" -Token $token -Body @{
    name = 'Emergency Fund Updated'
    targetAmount = 100000
    currentAmount = 25000
    targetDate = $nextYear.ToString('o')
    status = 1
}
Assert-True ($updatedGoal.success -and $updatedGoal.data.currentAmount -eq 25000) 'Update goal should succeed.'
$results.Add('Goals update')

$coachChat = Invoke-Api -Method Post -Path '/api/agents/chat' -Token $token -Body @{
    message = 'How can I improve my food spending?'
}
Assert-True ($coachChat.success -and $coachChat.data.agentUsed -eq 3) 'Coach chat should route to the coach agent.'
$results.Add('Agents chat coach')

$budgetChat = Invoke-Api -Method Post -Path '/api/agents/chat' -Token $token -Body @{
    message = 'Am I over budget this month?'
    budgetId = $budgetId
}
Assert-True ($budgetChat.success -and $budgetChat.data.agentUsed -eq 2) 'Budget chat should route to the budget agent.'
$results.Add('Agents chat budget')

$anomalyChat = Invoke-Api -Method Post -Path '/api/agents/chat' -Token $token -Body @{
    message = 'Does this look suspicious?'
    transactionId = $expenseTxnId
}
Assert-True ($anomalyChat.success -and $anomalyChat.data.agentUsed -eq 1) 'Anomaly chat should route to the anomaly agent.'
$results.Add('Agents chat anomaly')

$investmentChat = Invoke-Api -Method Post -Path '/api/agents/chat' -Token $token -Body @{
    message = 'How should I invest my monthly surplus?'
    riskProfile = 'moderate'
    age = 29
}
Assert-True ($investmentChat.success -and $investmentChat.data.agentUsed -eq 4) 'Investment chat should route to the investment agent.'
$results.Add('Agents chat investment')

$reportChat = Invoke-Api -Method Post -Path '/api/agents/chat' -Token $token -Body @{
    message = 'Give me my monthly report summary'
}
Assert-True ($reportChat.success -and $reportChat.data.agentUsed -eq 5) 'Report chat should route to the report agent.'
$results.Add('Agents chat report')

$coachWidget = Invoke-Api -Method Get -Path '/api/agents/widgets/coach' -Token $token
Assert-True ($coachWidget.success -and $coachWidget.data.healthScore -ge 25) 'Coach widget endpoint should return widget data.'
$results.Add('Agents widget coach')

$reportWidget = Invoke-Api -Method Get -Path '/api/agents/widgets/report' -Token $token
Assert-True ($reportWidget.success -and -not [string]::IsNullOrWhiteSpace($reportWidget.data.title)) 'Report widget endpoint should return widget data.'
$results.Add('Agents widget report')

# Dashboard
$summary = Invoke-Api -Method Get -Path '/api/dashboard/summary' -Token $token
Assert-True ($summary.success) 'Dashboard summary should succeed.'
Assert-True ($summary.data.totalIncome -eq 30000) 'Dashboard summary total income should be 30000.'
Assert-True ($summary.data.totalExpenses -eq 3000) 'Dashboard summary total expenses should be 3000 after update.'
Assert-True ($summary.data.transactionCount -eq 2) 'Dashboard summary transaction count should be 2.'
$results.Add('Dashboard summary')

$trend = Invoke-Api -Method Get -Path '/api/dashboard/spending-trend' -Token $token
Assert-True ($trend.success -and $trend.data.Count -ge 1) 'Dashboard spending trend should return points.'
$results.Add('Dashboard spending trend')

$breakdown = Invoke-Api -Method Get -Path '/api/dashboard/category-breakdown' -Token $token
Assert-True ($breakdown.success -and $breakdown.data.Count -ge 1) 'Dashboard category breakdown should return expense buckets.'
$results.Add('Dashboard category breakdown')

$budgetHealth = Invoke-Api -Method Get -Path '/api/dashboard/budget-health' -Token $token
Assert-True ($budgetHealth.success -and $budgetHealth.data.Count -ge 1) 'Dashboard budget health should return budgets.'
$results.Add('Dashboard budget health')

$goalProgress = Invoke-Api -Method Get -Path '/api/dashboard/goal-progress' -Token $token
Assert-True ($goalProgress.success -and $goalProgress.data.Count -ge 1) 'Dashboard goal progress should return goals.'
$results.Add('Dashboard goal progress')

$monthlyInsights = Invoke-Api -Method Get -Path '/api/insights/monthly?months=6' -Token $token
Assert-True ($monthlyInsights.success -and $monthlyInsights.data.cards.Count -ge 1) 'Monthly insights should return at least one insight card.'
$results.Add('Insights monthly')

$budgetRiskInsights = Invoke-Api -Method Get -Path '/api/insights/budget-risk' -Token $token
Assert-True ($budgetRiskInsights.success -and $budgetRiskInsights.data.cards.Count -ge 1) 'Budget-risk insights should return at least one insight card.'
$results.Add('Insights budget risk')

$anomalyInsights = Invoke-Api -Method Get -Path '/api/insights/anomalies' -Token $token
Assert-True ($anomalyInsights.success -and $anomalyInsights.data.cards.Count -ge 1) 'Anomaly insights should return at least one insight card.'
$results.Add('Insights anomalies')

$goalInsights = Invoke-Api -Method Get -Path '/api/insights/goals' -Token $token
Assert-True ($goalInsights.success -and $goalInsights.data.cards.Count -ge 1) 'Goal insights should return at least one insight card.'
$results.Add('Insights goals')

$auditLogs = Invoke-Api -Method Get -Path '/api/audit-logs?take=20' -Token $token
Assert-True ($auditLogs.success -and $auditLogs.data.Count -ge 10) 'Audit logs should return recorded mutations.'
Assert-True (($auditLogs.data | Where-Object { $_.entityName -eq 'Auth' }).Count -ge 2) 'Audit logs should include auth activity.'
Assert-True (($auditLogs.data | Where-Object { $_.entityName -eq 'Transaction' }).Count -ge 2) 'Audit logs should include transaction activity.'
$results.Add('Audit logs list')

# Delete flows after dashboard checks
$deleteExpenseTxn = Invoke-Api -Method Delete -Path "/api/transactions/$expenseTxnId" -Token $token
Assert-True $deleteExpenseTxn.success 'Delete expense transaction should succeed.'
$results.Add('Transactions delete')

$deleteBudget = Invoke-Api -Method Delete -Path "/api/budgets/$budgetId" -Token $token
Assert-True $deleteBudget.success 'Delete budget should succeed.'
$results.Add('Budgets delete')

$deleteGoal = Invoke-Api -Method Delete -Path "/api/goals/$goalId" -Token $token
Assert-True $deleteGoal.success 'Delete goal should succeed.'
$results.Add('Goals delete')

$result = [pscustomobject]@{
    success = $true
    email = $email
    verified = $results
    dashboard = [pscustomobject]@{
        totalIncome = $summary.data.totalIncome
        totalExpenses = $summary.data.totalExpenses
        netAmount = $summary.data.netAmount
        totalBalance = $summary.data.totalBalance
        transactionCount = $summary.data.transactionCount
    }
}

$result | ConvertTo-Json -Depth 10
