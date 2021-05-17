param (
	[string]$package = $null,
    [string]$path = 'Logs',
    [string]$cs
)

try {

    if ([System.String]::IsNullOrEmpty($package)) {
        $package = Get-Date -Format "yyyy-MM-dd-HH-mm-ss"
    }

    $scon = New-Object System.Data.SqlClient.SqlConnection
    $scon.ConnectionString = $cs
    $scon.Open()

    Get-ChildItem -File -Path $path | ForEach-Object {
        $c = Get-Content -Path $_.FullName -Encoding "utf8"
        try {
            $params = @()
            $params += New-Object System.Data.SqlClient.SqlParameter("@Package",$package)
            $params += New-Object System.Data.SqlClient.SqlParameter("@Name",$_.Name)

            if ($_.Name -match '^([0-9]{4}-[0-9]{2}-[0-9]{2}_[0-9]{2}-[0-9]{2}-[0-9]{2}-[0-9]{3,8})_([^_]*)_([^\._]*)(?:_([0-9]+))?(?:_([0-9]+)(?:[,\.0-9]*)?ms)?\.(.*)$') {
                $params += New-Object System.Data.SqlClient.SqlParameter("@RequestId", $Matches[1])
                $ts = [System.DateTime]::ParseExact($Matches[1], "yyyy-MM-dd_HH-mm-ss-fff", [System.Globalization.CultureInfo]::InvariantCulture)
                $params += New-Object System.Data.SqlClient.SqlParameter("@TS", $ts)
                $params += New-Object System.Data.SqlClient.SqlParameter("@Method", $Matches[2].Replace('-','/'))
                $params += New-Object System.Data.SqlClient.SqlParameter("@MessageType", $Matches[3])
                if (![System.String]::IsNullOrEmpty($Matches[4])) {
                    $params += New-Object System.Data.SqlClient.SqlParameter("@HttpStatusCode", [System.Int32]::Parse($Matches[4]))
                }
                if (![System.String]::IsNullOrEmpty($Matches[5])) {
                    $params += New-Object System.Data.SqlClient.SqlParameter("@ProcTime", [System.Int32]::Parse($Matches[5]))
                }
                $params += New-Object System.Data.SqlClient.SqlParameter("@DataType", $Matches[6])
            }

            $params += New-Object System.Data.SqlClient.SqlParameter("@Data",[string]$c)

            
            $cmd = New-Object System.Data.SqlClient.SqlCommand
            $cmd.Connection = $scon
            $cmd.CommandTimeout = 0
            $cmd.CommandType = [System.Data.CommandType]::StoredProcedure
            $cmd.CommandText = "dbo.ImportFile"

            ForEach ($p in $params) {
                $cmd.Parameters.Add($p) | Out-Null
            }

            $cmd.ExecuteNonQuery() | Out-Null
        }
        catch [Exception] {
            Write-Error $_.Exception.Message
        }
        finally {
            $cmd.Dispose()
        }

        Write-Host $_.Name
    }
}
catch [Exception] {
    Write-Error $_.Exception.Message
}
finally {
    $scon.Dispose()
}