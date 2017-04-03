<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet
    version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
    xmlns:msbuild="http://schemas.microsoft.com/developer/msbuild/2003">

    <xsl:param name="currentVersion" select="'0.9'"/>

    <!--
        Update the settings version to the version of the current XSL Transforms
    -->
    <xsl:template match="msbuild:Project/msbuild:PropertyGroup/msbuild:NBuildKitConfigurationVersion/text()">
        <xsl:value-of select="$currentVersion"/>
    </xsl:template>
</xsl:stylesheet>
