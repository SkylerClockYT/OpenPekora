import {v4 as uuidv4} from "uuid";

export const SOAP = (baseUrl: string, jobExpiration: number, finalScript: string) => {
    // REQUIRES leading slash (for some reason)
    return `
    <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:rob="${baseUrl}">
   <soapenv:Header/>
   <soapenv:Body>
      <rob:BatchJob>
         <rob:job>
            <rob:id>${uuidv4().toString()}</rob:id>
            <rob:expirationInSeconds>${jobExpiration}</rob:expirationInSeconds>
            <rob:cores>1</rob:cores>
         </rob:job>
         <rob:script>
            <rob:name>${uuidv4().toString()}</rob:name>
            <rob:script><![CDATA[
                ${finalScript}
            ]]></rob:script>
            ${/*<arguments>
                {Arguments}
            </arguments>*/null}
         </rob:script>
      </rob:BatchJob>
   </soapenv:Body>
</soapenv:Envelope>
`;
};