import express from "ultimate-express";
import Config from "../Utilities/Libraries/Config.js";
import Valid from "../Utilities/Middleware/ValidateDto.js";
import {AssetRenderRequest} from "../Utilities/Dto/Catalog.js";
import {Console} from "../Utilities/Libraries/CS.js";
import {QueueBox} from "../Utilities/Libraries/Queue.js";
import {BaseJson, RequestRCCBase, RequestRCCBaseXMLData} from "./BaseController.js";

import ImageTemplate from "../../scripts/Image.json" with {type: "json"};
import DecalTemplate from "../../scripts/Decal.json" with {type: "json"};
import ClothingTemplate from "../../scripts/Clothing.json" with {type: "json"};
import * as fs from "node:fs";
import {Imager} from "../Utilities/Libraries/Imager.js";
import Resp from "../Utilities/Libraries/Resp.js";

const router = express.Router();
const box = new QueueBox<any>(`ImageBox`, Config.Ports.RCC.Image);

router.post("/image", Valid(AssetRenderRequest), async (req, res) => {
    const assetUrl = `${Config.BaseUrl}/v1/asset?id=${req.body.assetId}`;
    let xml;
    if (req.body.isFace) {
        xml = JSON.parse(JSON.stringify(DecalTemplate));
        xml.Settings.Arguments[0] = assetUrl;
        xml.Settings.Arguments[2] = 1680;
        xml.Settings.Arguments[3] = 1680;
        xml.Settings.Arguments[4] = Config.BaseUrl;
    } else {
        xml = JSON.parse(JSON.stringify(ImageTemplate));
        xml.Settings.Arguments[0] = req.body.assetId;
        xml.Settings.Arguments[1] = Config.BaseUrl;
        xml.Settings.Arguments[3] = 600;
        xml.Settings.Arguments[4] = 600;
    }
    Console.Debug(`Queueing ${req.body.isFace ? "face" : "image"} request with AssetId ${req.body.assetId}`);
    return await box.Enqueue((port: number) => RequestRCCBase(
        req,
        res,
        xml,
        port,
        req.body.isFace ? "Face" : "Image"
    ));
});

router.post("/clothing", Valid(AssetRenderRequest), async (req, res) => {
    const xml: BaseJson = JSON.parse(JSON.stringify(ClothingTemplate));
    xml.Settings.Arguments[0] = `${Config.BaseUrl}` + `/asset?id=${req.body.assetId}`;
    xml.Settings.Arguments[2] = 420;
    xml.Settings.Arguments[3] = 420;
    xml.Settings.Arguments[4] = Config.BaseUrl;
    Console.Debug(`Queueing clothing request with AssetId ${req.body.assetId}`);
    return await box.Enqueue((port: number) => RequestRCCBase(
        req,
        res,
        xml,
        port,
        "Clothing"
    ));
});

router.post("/teeshirt", Valid(AssetRenderRequest), async (req, res) => {
    const xml: BaseJson = JSON.parse(JSON.stringify(ImageTemplate));
    xml.Settings.Arguments[0] = req.body.assetId;
    xml.Settings.Arguments[1] = Config.BaseUrl;
    xml.Settings.Arguments[3] = 600;
    xml.Settings.Arguments[4] = 600;
    Console.Debug(`Queueing TeeShirt request with AssetId ${req.body.assetId}`);
    const base64Image = await box.Enqueue((port: number) => RequestRCCBaseXMLData(
        req,
        res,
        xml,
        port,
        "TeeShirt",
        2
    ));
    Console.Debug(`Done rendering TeeShirt, applying  overlay...`);
    if (!base64Image) {
        Console.Debug(`TeeShirt render errored, returning...`);
        return;
    }

    try {
        const TeeShirtContent = await Imager.ReadAsync(fs.readFileSync("./TeeShirtTemplate.png"));
        const AssetContent = await Imager.ReadAsync(Buffer.from(base64Image, "base64"));
        const TeeShirtImage = TeeShirtContent.GetImage();
        const AssetImage = AssetContent.GetImage();
        if (!AssetImage || !TeeShirtImage)
            throw new Error("TeeShirtImage/AssetContent could not be read.");
        const { width, height, aspectRatio } = AssetContent;

        let newWidth, newHeight;
        if (width > height) {
            newWidth = 250;
            newHeight = Math.round(newWidth / aspectRatio);
        } else {
            newHeight = 250;
            newWidth = Math.round(newHeight / aspectRatio);
        }
        const ResizedAssetContent = await AssetImage.resize(newWidth, newHeight).toBuffer();
        const TeeShirtFinal = await TeeShirtImage.composite([{
                input: ResizedAssetContent,
                top: 85,
                left: 85,
            }]).png().toBuffer();
        return Resp(res, 200, "success", true, {data: TeeShirtFinal.toString("base64")});
        // return res.status(200).set("Content-Type", "image/png").send(TeeShirtFinal);
    } catch (e: any) {
        Console.Error(`TeeShirt overlay could not be applied for TeeShirt ${req.body.assetId}. Message:\n ${e.message}`);
        return Resp(res, 206, "TeeShirt overlay could not be applied.", false, { error: e.message, data: base64Image });
    }
});

export default router;