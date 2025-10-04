import {Response} from "ultimate-express";

export default (res: Response, status?: number | null, message?: string | null, success?: boolean | null, additionalData?: any | null) => {
    return res.status(status || 200).send({
        success: success === null || success === undefined ? true : success,
        message: message || "Success",
        ...additionalData
    });
};