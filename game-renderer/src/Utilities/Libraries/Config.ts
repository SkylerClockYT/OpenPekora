import * as path from "path";
import * as fs from "fs";
const projectDir = path.resolve(process.cwd());

export const Config: IConfig = JSON.parse(fs.readFileSync(path.join(projectDir, "/config.json")).toString()) as IConfig;

export default Config;

export interface IConfig {
    BaseUrl: string;
    RCCUrl: string;
    Debug: boolean,
    Ports: {
        Process: number;
        RCC: {
            All: number[];
            Player: number[];
            Image: number[];
            Place: number[];
            Catalog: number[];
        }
    },
    Paths: {
        RCCService: string;
    }
}
