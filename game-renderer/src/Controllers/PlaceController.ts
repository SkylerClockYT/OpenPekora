import express, {Request, Response} from "ultimate-express";
import Config from "../Utilities/Libraries/Config.js";
import Valid from "../Utilities/Middleware/ValidateDto.js";
import {QueueBox} from "../Utilities/Libraries/Queue.js";
import {BaseJson, RequestRCCBase} from "./BaseController.js";
import {PlaceRenderRequest} from "../Utilities/Dto/Catalog.js";
import {Console} from "../Utilities/Libraries/CS.js";

import PlaceTemplate from "../../scripts/Place.json" with {type: "json"};

const router = express.Router();
const box = new QueueBox<express.Response>(`PlaceBox`, Config.Ports.RCC.Place);

router.post("/thumbnail", Valid(PlaceRenderRequest), async (req: Request, res: Response) => {
    const xml: BaseJson = JSON.parse(JSON.stringify(PlaceTemplate));
    xml.Settings.Arguments[0] = `${Config.BaseUrl}/v1/asset?id=${req.body.placeId}`;
    xml.Settings.Arguments[2] = req.body.x;
    xml.Settings.Arguments[3] = req.body.y;
    xml.Settings.Arguments[4] = Config.BaseUrl;
    xml.Settings.Arguments[6] = Config.BaseUrl;
    Console.Log(`Queueing place thumbnail request with PlaceId ${req.body.placeId}`);
    return await box.Enqueue((port: number) => RequestRCCBase(
        req,
        res,
        xml,
        port,
        "Place thumbnail"
    ));
});

export default router;
