<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet
    version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
    xmlns:msbuild="http://schemas.microsoft.com/developer/msbuild/2003">

    <!--
        Update the pipeline version to the version of the current XSLT files
    -->
    <xsl:include href="version.xsl"/>

    <!--
        Copy all the items as they are.
    -->
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>

    <xsl:output method="xml" encoding="utf-8" indent="yes"/>
</xsl:stylesheet>
