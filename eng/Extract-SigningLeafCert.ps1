<#
.SYNOPSIS
  Extracts the leaf signing certificate from a signed .nupkg's
  embedded .signature.p7s and writes it as a .cer file.

.DESCRIPTION
  Used by the publish-rc and publish-stable jobs in .github/workflows/ci.yml
  so the same logic isn't duplicated across jobs.

  Writes the .cer next to the .nupkg, sets two GitHub Actions step outputs
  (`fingerprint`, `cer-path`) when $env:GITHUB_OUTPUT is present, and appends
  a Markdown summary block to $env:GITHUB_STEP_SUMMARY when present.

.PARAMETER ArtifactsDir
  Directory containing the signed .nupkg. Defaults to ".\artifacts".

.PARAMETER Version
  Version string used to name the .cer (signing-cert-<version>.cer).
#>
[CmdletBinding()]
param(
    [string] $ArtifactsDir = '.\artifacts',
    [Parameter(Mandatory = $true)] [string] $Version
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$nupkg = Get-ChildItem (Join-Path $ArtifactsDir '*.nupkg') | Select-Object -First 1
if (-not $nupkg) { throw "No signed .nupkg found in $ArtifactsDir" }

# Read .signature.p7s from the nupkg ZIP. Wrap both streams in try/finally so
# the entry stream is disposed even if CopyTo throws, and the MemoryStream is
# always disposed.
$zip = [System.IO.Compression.ZipFile]::OpenRead($nupkg.FullName)
try {
    $entry = $zip.GetEntry('.signature.p7s')
    if (-not $entry) { throw "No .signature.p7s in $($nupkg.Name) — package was not signed" }

    $entryStream = $null
    $ms = $null
    try {
        $entryStream = $entry.Open()
        $ms = New-Object System.IO.MemoryStream
        $entryStream.CopyTo($ms)
        $sigBytes = $ms.ToArray()
    } finally {
        if ($entryStream) { $entryStream.Dispose() }
        if ($ms)          { $ms.Dispose() }
    }
} finally {
    $zip.Dispose()
}

$cms = New-Object System.Security.Cryptography.Pkcs.SignedCms
$cms.Decode($sigBytes)
$leaf = $cms.SignerInfos[0].Certificate

$cerPath = Join-Path $ArtifactsDir "signing-cert-$Version.cer"
[System.IO.File]::WriteAllBytes(
    $cerPath,
    $leaf.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert))

$fp = $leaf.GetCertHashString('SHA256')
Write-Host "Leaf cert    : $($leaf.Subject)"
Write-Host "SHA-256      : $fp"
Write-Host "NotBefore    : $($leaf.NotBefore.ToString('o'))"
Write-Host "NotAfter     : $($leaf.NotAfter.ToString('o'))"
Write-Host "Wrote        : $cerPath"

if ($env:GITHUB_OUTPUT) {
    "fingerprint=$fp" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    "cer-path=$cerPath" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
}

if ($env:GITHUB_STEP_SUMMARY) {
    @"
## Signing certificate

| Field    | Value |
|----------|-------|
| Subject  | $($leaf.Subject) |
| Issuer   | $($leaf.Issuer) |
| SHA-256  | ``$fp`` |
| Valid    | $($leaf.NotBefore.ToString('u')) → $($leaf.NotAfter.ToString('u')) |

If nuget.org rejects the package with a fingerprint mismatch, register the
``signing-cert-$Version`` artifact (the ``.cer`` file) at https://www.nuget.org/account/Certificates.
"@ | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Append -Encoding utf8
}
