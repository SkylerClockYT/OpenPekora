import {plainToInstance} from "class-transformer";
import {validate} from "class-validator";
import {Request, Response, NextFunction} from "ultimate-express";
import Resp from "../Libraries/Resp.js";

function ValidateDto(dtoClass: any) {
    return async (req: Request, res: Response, next: NextFunction) => {
        const instance = plainToInstance(dtoClass, req.body);
        const errors = await validate(instance);
        if (errors.length > 0) {
            return Resp(res, 400, "", false, { errors });
        }
        req.body = instance;
        next();
    };
}

export default ValidateDto;
