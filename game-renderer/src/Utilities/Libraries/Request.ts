import axios from "axios";
import {SOAP} from "./SOAP.js";
import Config from "./Config.js";
import {BaseJson} from "../../Controllers/BaseController.js";
import {Console} from "./CS.js";

export const HttpRequest = async <T>(method: HttpMethod, url: URL, data: any): Promise<T> => {
    const isBrowser = typeof window !== "undefined";
    try {
        if (isBrowser)
            throw new Error("Browser isn't supported for Requests!");
        return await axios.request({
            method,
            url: url.toString(),
            data,
            maxRedirects: 3,
        }).then(res => res.data) as Promise<T>;
    } catch (e) {
        if (axios.isAxiosError(e)) {
            if (e?.response?.status && e.response.status !== 502) {
                return e.response as T;
            }
        }
        // @ts-ignore
        throw new Error(e);
    }
};

export const RCCRequest = async <T>(port: number, data: BaseJson, jobExpiration: number): Promise<T> => {
    try {
        const headers = {
            "Content-Type": "text/xml",
        };
        const xml = SOAP(Config.BaseUrl, jobExpiration, JSON.stringify(data));
        const response = await axios.request({
            method: HttpMethod.POST,
            url: `${Config.RCCUrl}:${port}`,
            timeout: jobExpiration * 1000,
            data: xml,
            maxRedirects: 3,
            headers,
        });
        return response.data as T;
    } catch (e: any) {
        if (axios.isAxiosError(e)) {
            if (e?.response?.status && e.response.status !== 502) {
                return e.response as T;
            }
        }
        //throw new Error(e);
        Console.Error(`Error occurred while requesting to RCC: ${e.message}`);
        return null as T;
    }
};

export enum HttpMethod {
    POST = "POST",
    GET = "GET",
    PATCH = "PATCH",
    DELETE = "DELETE",
    PUT = "PUT",
}

export class LuaValue {
    type!: string;
    value!: string;
}

export class BatchJobResultClass {
    type!: string;
    value?: string; // present only for LUA_TSTRING
    table?: {
        LuaValue: LuaValue[];
    };
}

export class SOAPEnvelope {
    Header?: any;
    Body!: SOAPBodyClass;
}

export class SOAPBodyClass {
    BatchJobResponse!: BatchJobResponseClass;
}

export class BatchJobResponseClass {
    BatchJobResult!: BatchJobResultClass[];
}

export class SOAPEnvelope2 {
    Header?: any;
    Body!: {
        BatchJobResponse: {
            BatchJobResult: BatchJobResultClass;
        };
    };
}
