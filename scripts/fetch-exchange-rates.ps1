Set-StrictMode -Version Latest

$baseUrl = "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/od/rates_of_exchange"
$filter = "record_date:gte:2020-01-01"
$fields = "record_date,country,currency,country_currency_desc,effective_date,exchange_rate"
$pageSize = 100
$pageNum = 1

$next = "&page[size]=$pageSize&page[number]=$pageNum"

$allData = [System.Collections.Generic.List[object]]::new()


do {
    $uri = "${baseUrl}?filter=$filter&fields=$fields&$next"
    Write-Host "Fetching page $next ($uri)..."

    $response = Invoke-RestMethod -Uri $uri -Method Get

    $allData.AddRange($response.data)

    $next = $response.links.next

    if ($next) {
        $next = [System.Uri]::UnescapeDataString($next)
    }
} while ($next)

$outputPath = Join-Path $PSScriptRoot ".." "api" "Data" "exchange-rates.json"
New-Item -ItemType Directory -Force -Path (Split-Path $outputPath) | Out-Null

$allData | ConvertTo-Json -Depth 5 | Set-Content -Path $outputPath -Encoding UTF8

Write-Host "Saved $($allData.Count) records to $outputPath"
