param (
    [string] $MiddlewareBaseUrl,
    [string] $MiddlewareSubscriptionKey,
    [string] $SchemaPath
)

$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
$headers.Add("Content-Type", 'application/json')
$headers.Add("Ocp-Apim-Subscription-Key", $MiddlewareFunctionsKey)

# TODO: Add OAuth token-getting.
$response = Invoke-RestMethod -TimeoutSec 10000 "$($MiddlewareBaseUrl)events" -Method Post -Headers $headers -Body "$(get-content $SchemaPath)"
Write-Host $response