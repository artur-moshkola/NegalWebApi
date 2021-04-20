try {

    $loc = Get-Location
    $bpe = "$($loc.Path)\"

    Get-ChildItem -File -Recurse -Path .\ | ForEach-Object {
        try {
            if ($_.FullName.Substring(0, $bpe.Length) -eq $bpe) {
                $rp = $_.FullName.Substring($bpe.Length).Replace("\", "-")
                if ($rp -match '\.(?:jpeg|jpg|png)$') {
                    $data = [System.IO.File]::ReadAllBytes($_.FullName);
                    $b64 = [System.Convert]::ToBase64String($data);
                    $b64 | Out-File -FilePath $rp'.b64' -NoNewline
                    Write-Host $rp
                }
            }
        }
        catch [Exception] {
            Write-Error $_.Exception.Message
        }

        
    }
}
catch [Exception] {
    Write-Error $_.Exception.Message
}