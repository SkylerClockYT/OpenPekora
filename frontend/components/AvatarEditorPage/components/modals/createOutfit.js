import {createUseStyles} from "react-jss";
import NewModal from "../../../newModal";
import ActionButton from "../../../actionButton";
import buttonStyles from "../../../../styles/buttonStyles";
import {createOutfit} from "../../../../services/avatar";
import AvatarPageStore from "../../stores/avatarPageStore";
import {useEffect, useRef, useState} from "react";
import feedback from "../../../../stores/feedback";
import FeedbackStore from "../../../../stores/feedback";
import {FeedbackType} from "../../../../models/feedback";

const useStyles = createUseStyles({
    footerClass: {
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
    },
    modalBtn: {
        padding: 9,
        margin: "0 5px",
        width: 90,
        fontSize: "18px!important",
        fontWeight: "400!important",
        lineHeight: "100%!important",
    },
    nameError: {
        margin: 0,
        height: 24,
        whiteSpace: "pre-line",
        lineHeight: "1.5em!important",
        color: "#D86868",
        wordWrap: "break-word",
        hyphens: "auto",
    },
    containerClass: {
        "& h5": {
            fontSize: 18,
            fontWeight: 400,
            lineHeight: "1em",
        }
    },
    inputStyle: {},
});

/**
 * @param {(boolean) => {}} openModal
 * @returns {JSX.Element}
 * @constructor
 */
const CreateOutfitModal = ({openModal}) => {
    const s = useStyles();
    const page = AvatarPageStore.useContainer();
    const btnStyles = buttonStyles();
    const [name, setName] = useState(null);
    const [nameHasError, setNameHasError] = useState(false);
    const feedback = FeedbackStore.useContainer();
    
    useEffect(() => {
        if (!/^[A-Z0-9 ]+$/i.test(name)) {
            setNameHasError(true);
        } else {
            setNameHasError(false);
        }
    }, [name])
    
    return <NewModal
        title="Create New Outfit"
        children={<>
            <p style={{margin: 0}}>An outfit will be created from your avatar's current appearance</p>
            <div>
                <input
                    type="text"
                    placeholder="Name your outfit"
                    onChange={e => setName(e.target.value)}
                    className={`inputTextStyle ${nameHasError ? "hasError" : ""} ${s.inputStyle}`}
                    maxLength={25}
                />
                {
                    nameHasError ? <p className={s.nameError}>Name can contain letters, numbers, and spaces</p> : null
                }
            </div>
        </>}
        footerElements={<>
            <ActionButton
                label="Create"
                buttonStyle={nameHasError ? btnStyles.newDisabledContinueButton : btnStyles.newContinueButton}
                onClick={async () => {
                    openModal(false);
                    await createOutfit({name});
                    await page.LoadOutfits();
                    feedback.addFeedback("Created outfit", FeedbackType.SUCCESS)
                    setName(null);
                }}
                className={s.modalBtn}
                disabled={nameHasError}
            />
            <ActionButton
                label="Cancel"
                buttonStyle={btnStyles.newCancelButton}
                onClick={() => openModal(false)}
                className={s.modalBtn}
            />
        </>}
        footerClass={s.footerClass}
        containerClass={s.containerClass}
    />
}

export default CreateOutfitModal;
